import logging
import time
from typing import Dict, Any

from fastapi import FastAPI
from pydantic import BaseModel, Field
import uvicorn

# Add parent directory to path for imports
import sys
import os
sys.path.append(os.path.dirname(__file__))

from config import HOST, PORT, TIMEOUT_MS
from service import punctuation_service

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

class FixRequest(BaseModel):
    text: str = Field(..., min_length=1, description="Text to fix punctuation and case")

class FixResponse(BaseModel):
    text_fixed: str = Field(..., description="Fixed text with punctuation and case")
    fallback: bool = Field(default=False, description="Whether fallback was used")

app = FastAPI(
    title="Sber Punctuation Service",
    description="HTTP service for punctuation and case restoration using Sber model",
    version="1.0.0"
)

@app.on_event("startup")
async def startup_event():
    """Initialize model on startup."""
    logger.info("Starting punctuation service...")
    try:
        # Pre-load the model to avoid delays on first request
        punctuation_service.initialize_model()
        # Test the model with a small text to ensure it's ready
        test_result = punctuation_service.process("test")
        logger.info(f"Model test completed: '{test_result}'")
        logger.info("Service ready")
    except Exception as e:
        logger.error(f"Failed to initialize service: {e}")
        raise

@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy"}

@app.post("/fix", response_model=FixResponse)
async def fix_text(request: FixRequest) -> Dict[str, Any]:
    """
    Fix punctuation and case in the provided text.

    Returns the processed text or original text with fallback=True on error.
    """
    start_time = time.time()
    input_size = len(request.text.encode('utf-8'))

    logger.info(f"Processing request: input_size={input_size} bytes")

    try:
        # TEMPORARILY DISABLED: Ensure model is initialized
        # if punctuation_service.model is None:
        #     punctuation_service.initialize_model()

        # Process the text
        fixed_text = punctuation_service.process(request.text)

        processing_time = time.time() - start_time
        logger.info(
            f"Request processed successfully: "
            f"input_size={input_size}, "
            f"processing_time={processing_time:.3f}s"
        )

        return FixResponse(text_fixed=fixed_text, fallback=False)

    except Exception as e:
        processing_time = time.time() - start_time
        logger.error(
            f"Request processing failed: "
            f"input_size={input_size}, "
            f"processing_time={processing_time:.3f}s, "
            f"error={str(e)}"
        )

        # Return original text with fallback flag
        return FixResponse(text_fixed=request.text, fallback=True)

if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host=HOST,
        port=PORT,
        workers=1,
        log_level="info"
    )
