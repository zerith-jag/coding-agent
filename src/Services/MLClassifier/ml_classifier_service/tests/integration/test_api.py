"""Integration tests for the classification API."""

import pytest
from fastapi.testclient import TestClient
from main import app


@pytest.fixture
def client():
    """Create a test client for the FastAPI app."""
    return TestClient(app)


class TestClassificationAPI:
    """Test suite for classification API endpoints."""

    def test_health_endpoint(self, client):
        """Test the health check endpoint."""
        response = client.get("/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "healthy"
        assert data["service"] == "ML Classifier"
        assert data["version"] == "2.0.0"

    def test_root_endpoint(self, client):
        """Test the root endpoint."""
        response = client.get("/")

        assert response.status_code == 200
        data = response.json()
        assert data["service"] == "ML Classifier"
        assert data["status"] == "running"

    def test_classify_bug_fix(self, client):
        """Test classification of a bug fix task."""
        request = {"task_description": "Fix the critical login bug affecting all users"}

        response = client.post("/classify/", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["task_type"] == "bug_fix"
        assert 0.0 <= data["confidence"] <= 1.0
        assert data["classifier_used"] == "heuristic"
        assert "reasoning" in data
        assert "suggested_strategy" in data
        assert data["estimated_tokens"] > 0

    def test_classify_feature(self, client):
        """Test classification of a feature task."""
        request = {
            "task_description": "Implement OAuth2 authentication with Google and GitHub providers"
        }

        response = client.post("/classify/", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["task_type"] == "feature"
        assert data["complexity"] in ["simple", "medium", "complex"]

    def test_classify_with_context(self, client):
        """Test classification with additional context."""
        request = {
            "task_description": "Fix the database connection issue",
            "context": {"repository": "backend-api", "priority": "high"},
            "files_changed": ["src/database/connection.py"],
        }

        response = client.post("/classify/", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["task_type"] in ["bug_fix", "feature"]

    def test_classify_batch(self, client):
        """Test batch classification endpoint."""
        requests = [
            {"task_description": "Fix the login bug"},
            {"task_description": "Add user profile feature"},
            {"task_description": "Write unit tests for auth module"},
        ]

        response = client.post("/classify/batch", json=requests)

        assert response.status_code == 200
        data = response.json()
        assert len(data) == 3
        assert all("task_type" in item for item in data)
        assert all("confidence" in item for item in data)

    def test_classify_empty_description(self, client):
        """Test classification with empty description."""
        request = {"task_description": ""}

        response = client.post("/classify/", json=request)

        # Should fail validation
        assert response.status_code == 422

    def test_classify_invalid_request(self, client):
        """Test classification with invalid request format."""
        request = {"invalid_field": "some value"}

        response = client.post("/classify/", json=request)

        # Should fail validation
        assert response.status_code == 422

    def test_classify_response_schema(self, client):
        """Test that response matches expected schema."""
        request = {"task_description": "Refactor the authentication service"}

        response = client.post("/classify/", json=request)

        assert response.status_code == 200
        data = response.json()

        # Verify all required fields are present
        required_fields = [
            "task_type",
            "complexity",
            "confidence",
            "reasoning",
            "suggested_strategy",
            "estimated_tokens",
        ]
        for field in required_fields:
            assert field in data, f"Missing required field: {field}"

        # Verify field types
        assert isinstance(data["task_type"], str)
        assert isinstance(data["complexity"], str)
        assert isinstance(data["confidence"], (int, float))
        assert isinstance(data["reasoning"], str)
        assert isinstance(data["suggested_strategy"], str)
        assert isinstance(data["estimated_tokens"], int)
