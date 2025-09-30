import os
from typing import Literal

# Server configuration
HOST: str = os.getenv("HOST", "127.0.0.1")
PORT: int = int(os.getenv("PORT", "5050"))
WORKERS: int = int(os.getenv("WORKERS", "1"))

# Model configuration
DEVICE: Literal["cpu", "cuda"] = os.getenv("DEVICE", "auto")  # auto, cpu, cuda
BATCH_SIZE: int = int(os.getenv("BATCH_SIZE", "1"))

# Timeout configuration (milliseconds)
TIMEOUT_MS: int = int(os.getenv("TIMEOUT_MS", "5000"))

# Request limits
MAX_TEXT_LENGTH: int = int(os.getenv("MAX_TEXT_LENGTH", "20000"))  # 20 KB limit
