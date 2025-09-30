import torch
import logging
from typing import Optional

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(__file__)))
sys.path.append(os.path.join(os.path.dirname(os.path.dirname(__file__)), 'sbert_punc_case_ru_src'))
from sbertpunccase import SbertPuncCase
from config import DEVICE, MAX_TEXT_LENGTH

logger = logging.getLogger(__name__)

class PunctuationService:
    def __init__(self):
        self.model: Optional[SbertPuncCase] = None
        self.device = self._detect_device()

    def _detect_device(self) -> str:
        """Detect available device based on configuration and hardware."""
        if DEVICE == "cpu":
            return "cpu"
        elif DEVICE == "cuda":
            if torch.cuda.is_available():
                return "cuda"
            else:
                logger.warning("CUDA requested but not available, falling back to CPU")
                return "cpu"
        else:  # auto
            return "cuda" if torch.cuda.is_available() else "cpu"

    def initialize_model(self):
        """Initialize the punctuation model."""
        if self.model is None:
            logger.info(f"Initializing SbertPuncCase model on device: {self.device}")
            try:
                self.model = SbertPuncCase().to(self.device)
                self.model.eval()
                logger.info("Model initialized successfully")
            except Exception as e:
                logger.error(f"Failed to initialize model: {e}")
                raise
        else:
            logger.debug("Model already initialized")

    def process(self, text: str) -> str:
        """
        Process text with punctuation and case restoration.

        Args:
            text: Input text to process

        Returns:
            Processed text with punctuation and case fixes
        """
        if not text or not text.strip():
            return text

        if len(text.encode('utf-8')) > MAX_TEXT_LENGTH:
            logger.warning(f"Text too long ({len(text.encode('utf-8'))} bytes), truncating")
            # Truncate to approximate character limit
            text = text[:MAX_TEXT_LENGTH // 2]  # Rough approximation

        if self.model is None:
            raise RuntimeError("Model not initialized. Call initialize_model() first.")

        try:
            result = self.model.punctuate(text)
            return result
        except Exception as e:
            logger.error(f"Error processing text: {e}")
            raise


# Global service instance
punctuation_service = PunctuationService()
