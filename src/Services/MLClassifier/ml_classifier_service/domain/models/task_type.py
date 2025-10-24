"""Task type and complexity enums for classification."""

from enum import Enum


class TaskType(str, Enum):
    """Types of coding tasks that can be classified."""
    
    BUG_FIX = "bug_fix"
    FEATURE = "feature"
    REFACTOR = "refactor"
    DOCUMENTATION = "documentation"
    TEST = "test"
    DEPLOYMENT = "deployment"


class TaskComplexity(str, Enum):
    """Complexity levels for tasks based on estimated lines of code."""
    
    SIMPLE = "simple"      # < 50 LOC
    MEDIUM = "medium"      # 50-200 LOC
    COMPLEX = "complex"    # > 200 LOC
