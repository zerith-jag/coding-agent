"""Pytest configuration and fixtures."""

import sys
from pathlib import Path

# Add parent directory to Python path to allow imports
parent_dir = Path(__file__).parent.parent
sys.path.insert(0, str(parent_dir))
