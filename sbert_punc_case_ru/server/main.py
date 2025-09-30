#!/usr/bin/env python3
"""
Main entry point for the punctuation service.
"""
import uvicorn
from app import app

if __name__ == "__main__":
    uvicorn.run(
        app,
        host="127.0.0.1",
        port=5050,
        workers=1,
        log_level="info"
    )
