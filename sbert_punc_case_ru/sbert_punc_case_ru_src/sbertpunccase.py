# -*- coding: utf-8 -*-

import argparse
from pathlib import Path
from typing import Iterable, List, Optional, Sequence, Union

import numpy as np
import torch
import torch.nn as nn
from transformers import AutoModelForTokenClassification, AutoTokenizer

# Прогнозируемые знаки препинания
PUNK_MAPPING = {".": "PERIOD", ",": "COMMA", "?": "QUESTION"}

# Прогнозируемый регистр LOWER - нижний регистр, UPPER - верхний регистр для первого символа,
# UPPER_TOTAL - верхний регистр для всех символов
LABELS_CASE = ["LOWER", "UPPER", "UPPER_TOTAL"]
# Добавим в пунктуацию метку O означающий отсутсвие пунктуации
LABELS_PUNC = ["O"] + list(PUNK_MAPPING.values())

# Сформируем метки на основе комбинаций регистра и пунктуации
LABELS_list = []
for case in LABELS_CASE:
    for punc in LABELS_PUNC:
        LABELS_list.append(f"{case}_{punc}")
LABELS = {label: i + 1 for i, label in enumerate(LABELS_list)}
LABELS["O"] = -100
INVERSE_LABELS = {i: label for label, i in LABELS.items()}

LABEL_TO_PUNC_LABEL = {
    label: label.split("_")[-1] for label in LABELS.keys() if label != "O"
}
LABEL_TO_CASE_LABEL = {
    label: "_".join(label.split("_")[:-1]) for label in LABELS.keys() if label != "O"
}


def token_to_label(token, label):
    if type(label) == int:
        label = INVERSE_LABELS[label]
    if label == "LOWER_O":
        return token
    if label == "LOWER_PERIOD":
        return token + "."
    if label == "LOWER_COMMA":
        return token + ","
    if label == "LOWER_QUESTION":
        return token + "?"
    if label == "UPPER_O":
        return token.capitalize()
    if label == "UPPER_PERIOD":
        return token.capitalize() + "."
    if label == "UPPER_COMMA":
        return token.capitalize() + ","
    if label == "UPPER_QUESTION":
        return token.capitalize() + "?"
    if label == "UPPER_TOTAL_O":
        return token.upper()
    if label == "UPPER_TOTAL_PERIOD":
        return token.upper() + "."
    if label == "UPPER_TOTAL_COMMA":
        return token.upper() + ","
    if label == "UPPER_TOTAL_QUESTION":
        return token.upper() + "?"
    if label == "O":
        return token


def decode_label(label, classes="all"):
    if classes == "punc":
        return LABEL_TO_PUNC_LABEL[INVERSE_LABELS[label]]
    if classes == "case":
        return LABEL_TO_CASE_LABEL[INVERSE_LABELS[label]]
    else:
        return INVERSE_LABELS[label]


DEFAULT_MODEL_PATH = Path(__file__).resolve().parent.parent


class SbertPuncCase(nn.Module):
    def __init__(
        self,
        model_path: Optional[Union[str, Path]] = None,
        device: str = "auto",
    ) -> None:
        super().__init__()

        resolved_path = Path(model_path) if model_path else DEFAULT_MODEL_PATH
        if not resolved_path.exists():
            raise FileNotFoundError(f"Model path not found: {resolved_path}")

        self._device = self._resolve_device(device)
        self.tokenizer = AutoTokenizer.from_pretrained(resolved_path, strip_accents=False)
        self.model = AutoModelForTokenClassification.from_pretrained(resolved_path)
        self.model.to(self._device)
        self.model.eval()
        self._max_token_length = 512

    def forward(self, input_ids, attention_mask):
        return self.model(input_ids=input_ids, attention_mask=attention_mask)

    def punctuate(self, text: str) -> str:
        text = text.strip()
        if not text:
            return text

        words = text.split()
        if not words:
            return text

        processed_segments: List[str] = []
        for chunk in self._split_words(words):
            processed_segments.append(self._punctuate_chunk(chunk))

        return " ".join(segment for segment in processed_segments if segment)

    def _punctuate_chunk(self, words: Sequence[str]) -> str:
        tokenizer_output = self.tokenizer(list(words), is_split_into_words=True)

        input_ids = torch.tensor([tokenizer_output.input_ids], device=self._device)
        attention_mask = torch.tensor(
            [tokenizer_output.attention_mask], device=self._device
        )

        with torch.inference_mode():
            logits = self.model(input_ids=input_ids, attention_mask=attention_mask).logits

        predictions = logits.cpu().numpy()
        predictions = np.argmax(predictions, axis=2)

        result_tokens: List[str] = []
        word_ids = tokenizer_output.word_ids()
        for i, word in enumerate(words):
            try:
                label_pos = word_ids.index(i)  # type: ignore[arg-type]
            except ValueError:
                result_tokens.append(word)
                continue
            label_id = predictions[0][label_pos]
            label = decode_label(label_id)
            result_tokens.append(token_to_label(word, label))

        return " ".join(result_tokens)

    def _split_words(self, words: Sequence[str]) -> Iterable[Sequence[str]]:
        if not words:
            return

        chunk: List[str] = []
        for word in words:
            tentative = chunk + [word]
            tokenized = self.tokenizer(tentative, is_split_into_words=True)
            if len(tokenized.input_ids) > self._max_token_length and chunk:
                yield list(chunk)
                chunk = [word]
            else:
                chunk = tentative

        if chunk:
            yield list(chunk)

    @staticmethod
    def _resolve_device(device: str) -> torch.device:
        requested = device.lower()
        if requested == "cuda":
            if torch.cuda.is_available():
                return torch.device("cuda")
            raise RuntimeError("CUDA requested but not available")
        if requested == "auto":
            if torch.cuda.is_available():
                return torch.device("cuda")
            return torch.device("cpu")
        return torch.device("cpu")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        "Punctuation and case restoration model sbert_punc_case_ru"
    )
    parser.add_argument(
        "-i",
        "--input",
        type=str,
        help="text to restore",
        default="sbert punc case расставляет точки запятые и знаки вопроса вам нравится",
    )
    parser.add_argument(
        "-d",
        "--device",
        type=str,
        help="run model on cpu or gpu",
        choices=["cpu", "cuda"],
        default="cpu",
    )
    args = parser.parse_args()
    print(f"Source text:   {args.input}\n")
    sbertpunc = SbertPuncCase(device=args.device)
    punctuated_text = sbertpunc.punctuate(args.input)
    print(f"Restored text: {punctuated_text}")
