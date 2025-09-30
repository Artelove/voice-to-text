import logging
import time
from typing import Dict

from fastapi import FastAPI
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field, validator

configure_logging()
logger = logging.getLogger("postprocessor")
settings = get_settings()

app = FastAPI(title="VoiceToText Post-Processor", version="1.0.0")


class FixRequest(BaseModel):
    text: str = Field(..., min_length=1, max_length=settings.max_request_chars)

    @validator("text")
    def _trim(cls, value: str) -> str:
        return value.strip()


class FixResponse(BaseModel):
    text_fixed: str
    fallback: bool = False
    latency_ms: float | None = None
    corrections: int | None = None


@app.on_event("startup")
def on_startup() -> None:
    try:
        logger.info("Service starting on %s:%d", settings.host, settings.port)
        punctuator = SbertPuncCase(model_path=settings.model_dir, device=settings.device)
        spell_corrector = SpellCorrector(language=settings.spell_language)
        app.state.pipeline = TextPipeline(
            punctuator=punctuator,
            spell_corrector=spell_corrector,
            segment_word_length=settings.segment_word_length,
        )
    except Exception as exc:  # pragma: no cover - startup failure is critical
        logger.exception("Failed to initialize pipeline: %s", exc)
        raise


@app.get(settings.health_path)
def health() -> Dict[str, str]:
    return {"status": "ok"}


@app.post(settings.fix_path, response_model=FixResponse)
def fix_text(request: FixRequest) -> JSONResponse:
    pipeline: TextPipeline = app.state.pipeline

    start = time.perf_counter()
    try:
        processed, metrics = pipeline.process(request.text)
    except Exception as exc:
        logger.exception("Failed to process text: %s", exc)
        return JSONResponse(
            FixResponse(text_fixed=request.text, fallback=True).dict(),
            status_code=200,
        )

    elapsed_ms = (time.perf_counter() - start) * 1000
    payload = FixResponse(
        text_fixed=processed,
        fallback=False,
        latency_ms=elapsed_ms,
        corrections=metrics.corrections,
    ).dict()
    logger.info(
        "POST /fix processed len=%d total_latency=%.0fms pipeline_latency=%.0fms corrections=%d",
        len(request.text),
        elapsed_ms,
        metrics.elapsed_ms,
        metrics.corrections,
    )
    return JSONResponse(payload)


__all__ = ["app", "FixRequest", "FixResponse"]