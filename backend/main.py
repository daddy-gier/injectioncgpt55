import os
import json
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
from pydantic import BaseModel
from typing import Optional
import anthropic
from system_prompt import UNREAL_EXPERT_SYSTEM_PROMPT

app = FastAPI(title="Unreal Engine AI Expert")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

client = anthropic.Anthropic(api_key=os.environ.get("ANTHROPIC_API_KEY"))

CONVERSATION_STORE: dict[str, list] = {}


class ChatRequest(BaseModel):
    message: str
    session_id: str = "default"
    ue_version: Optional[str] = "5.4"


class ChatResponse(BaseModel):
    reply: str
    session_id: str


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/chat", response_model=ChatResponse)
def chat(req: ChatRequest):
    history = CONVERSATION_STORE.setdefault(req.session_id, [])

    version_context = f"\n\nThe user is working with Unreal Engine {req.ue_version}." if req.ue_version else ""
    system = UNREAL_EXPERT_SYSTEM_PROMPT + version_context

    history.append({"role": "user", "content": req.message})

    try:
        response = client.messages.create(
            model="claude-opus-5-20251101",
            max_tokens=4096,
            system=system,
            messages=history,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

    reply = response.content[0].text
    history.append({"role": "assistant", "content": reply})

    # Keep last 20 turns to avoid runaway token costs
    if len(history) > 40:
        CONVERSATION_STORE[req.session_id] = history[-40:]

    return ChatResponse(reply=reply, session_id=req.session_id)


@app.delete("/chat/{session_id}")
def clear_session(session_id: str):
    CONVERSATION_STORE.pop(session_id, None)
    return {"cleared": session_id}


@app.get("/sessions")
def list_sessions():
    return {"sessions": list(CONVERSATION_STORE.keys())}


# Serve frontend
frontend_path = os.path.join(os.path.dirname(__file__), "..", "frontend")
if os.path.isdir(frontend_path):
    app.mount("/static", StaticFiles(directory=frontend_path), name="static")

    @app.get("/")
    def root():
        return FileResponse(os.path.join(frontend_path, "index.html"))
