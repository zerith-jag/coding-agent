"""Unit tests for heuristic classifier."""

import pytest
from ml_classifier_service.domain.classifiers.heuristic import HeuristicClassifier
from ml_classifier_service.domain.models.task_type import TaskType, TaskComplexity


@pytest.fixture
def classifier():
    """Create a heuristic classifier instance."""
    return HeuristicClassifier()


class TestHeuristicClassifier:
    """Test suite for heuristic classifier."""
    
    def test_classify_bug_fix(self, classifier):
        """Test classification of a bug fix task."""
        description = "Fix the login bug where users can't authenticate"
        result = classifier.classify(description)
        
        assert result.task_type == TaskType.BUG_FIX
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
        assert "bug" in result.reasoning.lower() or "fix" in result.reasoning.lower()
    
    def test_classify_feature(self, classifier):
        """Test classification of a feature task."""
        description = "Implement a new user registration feature with email verification"
        result = classifier.classify(description)
        
        assert result.task_type == TaskType.FEATURE
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
    
    def test_classify_refactor(self, classifier):
        """Test classification of a refactor task."""
        description = "Refactor the authentication module to improve code quality"
        result = classifier.classify(description)
        
        assert result.task_type == TaskType.REFACTOR
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
    
    def test_classify_test(self, classifier):
        """Test classification of a test task."""
        description = "Write unit tests for the user service with 90% coverage"
        result = classifier.classify(description)
        
        assert result.task_type == TaskType.TEST
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
    
    def test_classify_documentation(self, classifier):
        """Test classification of a documentation task."""
        description = "Update the README with installation instructions and examples"
        result = classifier.classify(description)
        
        assert result.task_type == TaskType.DOCUMENTATION
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
    
    def test_classify_deployment(self, classifier):
        """Test classification of a deployment task."""
        description = "Deploy the application to Kubernetes cluster with Helm charts"
        result = classifier.classify(description)
        
        assert result.task_type == TaskType.DEPLOYMENT
        assert result.confidence > 0.5
        assert result.classifier_used == "heuristic"
    
    def test_classify_simple_complexity(self, classifier):
        """Test classification of simple task complexity."""
        description = "Fix a small typo in the login form"
        result = classifier.classify(description)
        
        assert result.complexity == TaskComplexity.SIMPLE
        assert result.suggested_strategy == "SingleShot"
        assert result.estimated_tokens == 2000
    
    def test_classify_medium_complexity(self, classifier):
        """Test classification of medium task complexity."""
        description = "Implement user authentication with JWT tokens, password hashing, and session management"
        result = classifier.classify(description)
        
        assert result.complexity in [TaskComplexity.SIMPLE, TaskComplexity.MEDIUM]
        assert result.suggested_strategy in ["SingleShot", "Iterative"]
    
    def test_classify_complex_complexity(self, classifier):
        """Test classification of complex task complexity."""
        description = (
            "Implement a complex microservices architecture with API gateway, "
            "multiple backend services, message queues, caching layer, "
            "database sharding, and comprehensive monitoring. "
            "This is a major refactor that will touch the entire system."
        )
        result = classifier.classify(description)
        
        assert result.complexity == TaskComplexity.COMPLEX
        assert result.suggested_strategy == "MultiAgent"
        assert result.estimated_tokens == 20000
    
    def test_classify_no_matches(self, classifier):
        """Test classification when no keywords match."""
        description = "Do something unspecified"
        result = classifier.classify(description)
        
        # Should default to FEATURE with low confidence
        assert result.task_type == TaskType.FEATURE
        assert result.confidence < 0.5
        assert result.classifier_used == "heuristic"
    
    def test_classify_multiple_matches(self, classifier):
        """Test classification with multiple keyword matches."""
        description = "Fix the bug in the new feature implementation"
        result = classifier.classify(description)
        
        # Should pick the type with most matches
        assert result.task_type in [TaskType.BUG_FIX, TaskType.FEATURE]
        assert result.confidence > 0.0
    
    def test_confidence_score_range(self, classifier):
        """Test that confidence scores are within valid range."""
        descriptions = [
            "Fix the login bug",
            "Add new feature",
            "Refactor code",
            "Write tests",
            "Update documentation",
            "Deploy to production"
        ]
        
        for description in descriptions:
            result = classifier.classify(description)
            assert 0.0 <= result.confidence <= 1.0
    
    def test_estimated_tokens_positive(self, classifier):
        """Test that estimated tokens are always positive."""
        description = "Fix a bug in the authentication system"
        result = classifier.classify(description)
        
        assert result.estimated_tokens > 0
