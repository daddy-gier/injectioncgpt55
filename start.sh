#!/bin/bash
set -e

if [ -z "$ANTHROPIC_API_KEY" ]; then
  echo "ERROR: ANTHROPIC_API_KEY is not set."
  echo "  export ANTHROPIC_API_KEY=sk-ant-..."
  exit 1
fi

cd backend
pip install -r requirements.txt -q
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
