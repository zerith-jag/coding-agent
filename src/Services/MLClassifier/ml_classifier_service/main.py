"""Main FastAPI application for ML Classifier service."""

import logging
from contextlib import asynccontextmanager

from api.routes import classification, health
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Lifespan context manager for startup and shutdown events."""
    # Startup
    logger.info("Starting ML Classifier Service...")
    logger.info("Heuristic classifier initialized")
    # Future: Load ML models, connect to database, start event consumers

    yield

    # Shutdown
    logger.info("Shutting down ML Classifier Service...")
    # Future: Close database connections, stop event consumers


# Create FastAPI app
app = FastAPI(
    title="ML Classifier Service",
    description="Task classification service using heuristic and ML approaches",
    version="2.0.0",
    lifespan=lifespan,
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(health.router)
app.include_router(classification.router)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True, log_level="info")
