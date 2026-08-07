# injectioncgpt55

Drop-in Unity Editor scripts and a local-LLM integration, put together
from a console-log troubleshooting session. This repo does **not**
contain a full Unity project (no `ProjectSettings/`, no `Library/`) —
just scripts meant to be copied into an existing project's `Assets`
folder.

## What's here

### `Assets/Editor/` — one-click fixes

- **`FixObsoleteMaterialLocation.cs`** —
  `Tools ▸ Unity Fixes ▸ Fix Obsolete External Material Location (Whole Project)`.
  Batch-switches every model importer still using the deprecated
  "Use External Materials (Legacy)" setting over to "Use Embedded
  Materials", clearing the `MaterialLocation.External is obsolete`
  console warning across every FBX in the project in one click.
- **`RegenerateLightingMenu.cs`** —
  `Tools ▸ Unity Fixes ▸ Regenerate Lighting (Bake, Blocking / Async)`.
  Scripted equivalent of `Window ▸ Rendering ▸ Lighting ▸ Generate
  Lighting`, for triggering a rebake from a menu (or a batch/CI script)
  instead of hunting through the Lighting window — this is what clears
  the "Lighting data asset is incompatible" warnings.

### `Assets/OllamaGptOss/` — local LLM integration (Ollama + gpt-oss:20b)

A minimal client for talking to a local [Ollama](https://ollama.com)
server from inside Unity, defaulting to OpenAI's open-weight
`gpt-oss:20b` model.

- `Runtime/OllamaClient.cs` — `MonoBehaviour` wrapping Ollama's REST API
  (`/api/generate`, `/api/chat`, `/api/tags`), both non-streaming and
  streaming, via `UnityWebRequest` coroutines.
- `Runtime/OllamaModels.cs` — request/response data classes.
- `Runtime/OllamaStreamingHandler.cs` — custom `DownloadHandlerScript`
  that parses Ollama's newline-delimited JSON stream chunk by chunk (for
  a typewriter-style effect).
- `Samples/OllamaChatDemo.cs` — dependency-free demo component (no UI
  package required). Add it next to `OllamaClient` on a GameObject, enter
  Play mode, right-click the component → **Send Test Prompt**, watch the
  Console.
- `Editor/OllamaSetupWindow.cs` — `Window ▸ Ollama GPT-OSS ▸ Setup
  Check`. Pings your local Ollama server and can launch
  `ollama pull gpt-oss:20b` for you from inside the Editor.

#### Setup

1. Install Ollama: <https://ollama.com/download> (Windows/macOS/Linux).
2. Pull the model (~13 GB download):
   ```
   ollama pull gpt-oss:20b
   ```
   16 GB+ of RAM/VRAM is recommended for reasonable speed (there's also a
   heavier `gpt-oss:120b` if your hardware can take it). If 20b is too
   much for your machine, point `OllamaClient.model` at something
   lighter instead (e.g. `llama3.2:3b`) — the model name isn't hardcoded
   anywhere else in the client.
3. Confirm the server is reachable — `ollama list` should show
   `gpt-oss:20b` once the pull finishes. The installer runs the server
   in the background automatically on Windows/macOS; otherwise start it
   with `ollama serve`.
4. Copy `Assets/OllamaGptOss` (and `Assets/Editor`, if you want the
   fixes too) into your project's `Assets` folder.
5. Add both `OllamaClient` and `OllamaChatDemo` to any GameObject, press
   Play, right-click `OllamaChatDemo` in the Inspector → **Send Test
   Prompt**.

This talks to `localhost:11434`, so it works for Editor and Standalone
(PC/Mac/Linux) builds on the same machine, or across a LAN if you point
`host` at another machine's IP. It will **not** work out of the box for
WebGL or mobile builds unless `host` points at a server they can actually
reach — a browser or phone can't resolve "localhost" as the machine
running your Unity Editor.

## About the rest of the original triage list

Two of the four original fix-it steps can't be scripted or run from a
repo — they're either local OS/network configuration or a manual Editor
action tied to whichever specific package is affected on your machine:

1. **Network/firewall resetting Package Manager downloads**
   (`Recv failure: Connection was reset`) — OS/antivirus/VPN
   configuration on your machine; check firewall/AV web-shield rules and
   retry the download.
2. **Re-importing the package the broken prefabs belong to** — depends
   on which specific Asset Store package failed to fully import (check
   the Hierarchy for "Missing Prefab"). Re-download/reimport it via
   Package Manager once #1 is sorted.

Steps 3 and 4 from that list are handled by `FixObsoleteMaterialLocation.cs`
and `RegenerateLightingMenu.cs` above.

## `.gitignore`

A standard Unity `.gitignore` is included so that if you do push a full
Unity project into this repo later, `Library/`, `Temp/`, `Logs/`, and
other generated folders won't get committed — that's exactly the kind of
thing that causes "Lighting data asset is incompatible" pain for
collaborators using a different Unity version.
