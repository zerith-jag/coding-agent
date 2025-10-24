"""Health check and service status routes."""

from fastapi import APIRouter
from pydantic import BaseModel


router = APIRouter(prefix="", tags=["Health"])


class HealthResponse(BaseModel):
    """Health check response model."""
    status: str
    service: str
    version: str


@router.get("/health", response_model=HealthResponse)
async def health_check() -> HealthResponse:
    """
    Health check endpoint.
    
    Returns service status and version information.
    """
    return HealthResponse(
        status="healthy",
        service="ML Classifier",
        version="2.0.0"
    )


@router.get("/", response_model=dict)
async def root() -> dict:
    """
    Root endpoint with service information.
    
    Returns basic service metadata.
    """
    return {
        "service": "ML Classifier",
        "version": "2.0.0",
        "status": "running",
        "description": "Task classification service using heuristic and ML approaches"
    }
