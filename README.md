# Unreal Engine AI Expert

An AI that thinks like the engineers who built Unreal Engine. Deep knowledge of every UE subsystem — rendering, physics, networking, animation, GAS, World Partition, Nanite, Lumen, and everything else.

## Quick Start

### 1. Get an Anthropic API key
Sign up at [console.anthropic.com](https://console.anthropic.com) and create an API key.

### 2. Set your API key
```bash
export ANTHROPIC_API_KEY=sk-ant-your-key-here
```

### 3. Run
```bash
chmod +x start.sh
./start.sh
```

### 4. Open the chat
Go to [http://localhost:8000](http://localhost:8000)

---

## What it knows

- **Core Engine**: UObject system, GC, tick hierarchy, GameFramework
- **C++ & Blueprints**: Macros, delegates, UBT, reflection, Blueprint VM
- **Rendering**: Nanite, Lumen, VSM, RHI, materials, shaders, RDG
- **Physics**: Chaos, cloth, vehicles, character movement
- **Animation**: AnimBP, Control Rig, Motion Warping, Sequencer
- **Networking**: Replication, RPCs, Iris, Online Subsystem
- **AI**: Behavior Trees, GAS, EQS, Mass Entity, StateTree
- **World Building**: World Partition, Landscape, PCG, Level Streaming
- **Performance**: Unreal Insights, GPU profiling, draw calls, memory
- **Pipeline**: Asset Manager, DDC, Cooking, Commandlets, Editor tools

## Stack

- **Backend**: Python + FastAPI + Anthropic Claude Opus 5
- **Frontend**: Vanilla HTML/CSS/JS (no build step needed)
- **Memory**: In-memory conversation history per session

## Project Structure

```
├── backend/
│   ├── main.py           # FastAPI server + chat endpoint
│   ├── system_prompt.py  # The UE expert identity & knowledge
│   └── requirements.txt
├── frontend/
│   └── index.html        # Chat UI
└── start.sh              # One-command launcher
```