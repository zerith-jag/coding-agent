"""Classification routes for task classification API."""

from fastapi import APIRouter, HTTPException, status
from ...api.schemas.classification import ClassificationRequest, ClassificationResult
from ...domain.classifiers.heuristic import HeuristicClassifier
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/classify", tags=["Classification"])

# Initialize the heuristic classifier (singleton)
heuristic_classifier = HeuristicClassifier()


@router.post("/", response_model=ClassificationResult)
async def classify_task(request: ClassificationRequest) -> ClassificationResult:
    """
    Classify a coding task.
    
    Uses heuristic classification based on keyword matching.
    Returns task type, complexity, confidence, and execution recommendations.
    
    Args:
        request: Classification request with task description
        
    Returns:
        Classification result with task type, complexity, and recommendations
        
    Raises:
        HTTPException: If classification fails
    """
    try:
        logger.info(f"Classifying task: {request.task_description[:50]}...")
        
        # Use heuristic classifier (Phase 1 - ML/LLM will be added later)
        result = heuristic_classifier.classify(request.task_description)
        
        logger.info(
            f"Classification complete: type={result.task_type.value}, "
            f"complexity={result.complexity.value}, confidence={result.confidence:.2f}"
        )
        
        return result
        
    except Exception as e:
        logger.error(f"Classification failed: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Classification failed: {str(e)}"
        )


@router.post("/batch", response_model=list[ClassificationResult])
async def classify_tasks_batch(requests: list[ClassificationRequest]) -> list[ClassificationResult]:
    """
    Classify multiple tasks in batch.
    
    Args:
        requests: List of classification requests
        
    Returns:
        List of classification results
        
    Raises:
        HTTPException: If batch classification fails
    """
    try:
        logger.info(f"Batch classifying {len(requests)} tasks...")
        
        results = []
        for request in requests:
            result = heuristic_classifier.classify(request.task_description)
            results.append(result)
        
        logger.info(f"Batch classification complete: {len(results)} tasks processed")
        
        return results
        
    except Exception as e:
        logger.error(f"Batch classification failed: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Batch classification failed: {str(e)}"
        )
