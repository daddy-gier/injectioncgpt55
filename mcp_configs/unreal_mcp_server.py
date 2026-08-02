"""
unreal_mcp_server.py — MCP server bridge for Unreal Engine 5.8
Save to: C:\\Users\\leegi\\AppData\\Local\\Programs\\Python\\Python312\\Scripts\\unreal_mcp_server.py

This exposes UE editor commands over the MCP protocol so OpenClaw / Hermes
can directly control the Unreal Editor from AI tools.

The UE editor must be running with the Remote Control plugin enabled:
  Edit > Plugins > Remote Control API (enable, restart editor)
  Project Settings > Plugins > Remote Control > HTTP Server: Port 30010
"""

import json
import sys
import os
import asyncio
import httpx
from typing import Any

UE_HTTP_PORT = int(os.getenv("UE_EDITOR_PORT", "30010"))
UE_BASE = f"http://localhost:{UE_HTTP_PORT}/remote/object"
PROJECT_PATH = os.getenv("UE_PROJECT_PATH", r"F:\Unreal\NyghtshadeHollow_UE58")


async def ue_call(object_path: str, function: str, parameters: dict = None) -> dict:
    """Call a UE Remote Control API function."""
    payload = {
        "objectPath": object_path,
        "functionName": function,
        "parameters": parameters or {},
        "generateTransaction": True
    }
    async with httpx.AsyncClient(timeout=30) as client:
        resp = await client.put(f"{UE_BASE}/call", json=payload)
        resp.raise_for_status()
        return resp.json()


async def run_python_script(script: str) -> str:
    """Execute Python in the UE editor via Remote Python."""
    payload = {
        "objectPath": "/Engine/PythonScriptPlugin.Default__PythonScriptPlugin",
        "functionName": "ExecPythonCommand",
        "parameters": {"PythonCommand": script},
        "generateTransaction": False
    }
    async with httpx.AsyncClient(timeout=60) as client:
        resp = await client.put(f"{UE_BASE}/call", json=payload)
        return resp.text


# ── MCP Tool Handlers ─────────────────────────────────────────────────────────

TOOLS = [
    {
        "name": "ue_import_fbx",
        "description": "Import an FBX file into the UE Content Browser",
        "inputSchema": {
            "type": "object",
            "properties": {
                "fbx_path": {"type": "string", "description": "Absolute path to .fbx file"},
                "destination": {"type": "string", "description": "Content Browser path e.g. /Game/PrisonAssets/StaticMeshes"}
            },
            "required": ["fbx_path", "destination"]
        }
    },
    {
        "name": "ue_run_python",
        "description": "Run a Python script or statement inside the UE Editor",
        "inputSchema": {
            "type": "object",
            "properties": {
                "script": {"type": "string", "description": "Python code or path to .py file"}
            },
            "required": ["script"]
        }
    },
    {
        "name": "ue_create_blueprint",
        "description": "Create a Blueprint asset in the Content Browser",
        "inputSchema": {
            "type": "object",
            "properties": {
                "parent_class": {"type": "string"},
                "asset_name": {"type": "string"},
                "destination": {"type": "string"}
            },
            "required": ["parent_class", "asset_name", "destination"]
        }
    },
    {
        "name": "ue_get_project_info",
        "description": "Get current Unreal project info, asset counts, and folder structure",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "ue_save_all",
        "description": "Save all dirty packages in the UE Editor",
        "inputSchema": {"type": "object", "properties": {}}
    }
]


async def handle_tool(name: str, arguments: dict) -> str:
    if name == "ue_run_python":
        result = await run_python_script(arguments["script"])
        return result

    elif name == "ue_import_fbx":
        script = f"""
import unreal
task = unreal.AssetImportTask()
task.set_editor_property('automated', True)
task.set_editor_property('filename', r'{arguments["fbx_path"]}')
task.set_editor_property('destination_path', '{arguments["destination"]}')
task.set_editor_property('replace_existing', True)
fbx_opts = unreal.FbxImportUI()
fbx_opts.set_editor_property('import_materials', True)
fbx_opts.set_editor_property('import_textures', True)
fbx_opts.static_mesh_import_data.set_editor_property('auto_generate_collision', True)
task.set_editor_property('options', fbx_opts)
unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])
print('Import complete')
"""
        return await run_python_script(script)

    elif name == "ue_create_blueprint":
        script = f"""
import unreal
factory = unreal.BlueprintFactory()
factory.set_editor_property('parent_class', unreal.load_class(None, '{arguments["parent_class"]}'))
asset_tools = unreal.AssetToolsHelpers.get_asset_tools()
bp = asset_tools.create_asset('{arguments["asset_name"]}', '{arguments["destination"]}', unreal.Blueprint, factory)
print(f'Created: {{bp.get_path_name() if bp else "FAILED"}}')
"""
        return await run_python_script(script)

    elif name == "ue_save_all":
        script = "import unreal; unreal.EditorLoadingAndSavingUtils.save_dirty_packages(False, True); print('Saved')"
        return await run_python_script(script)

    elif name == "ue_get_project_info":
        script = f"""
import unreal, os
assets = unreal.EditorAssetLibrary.list_assets('/Game', recursive=True)
print(f'Project: {PROJECT_PATH}')
print(f'Total assets: {{len(assets)}}')
for path in ['/Game/PrisonAssets', '/Game/Blueprints', '/Game/UI']:
    sub = [a for a in assets if a.startswith(path)]
    print(f'  {{path}}: {{len(sub)}} assets')
"""
        return await run_python_script(script)

    return f"Unknown tool: {name}"


# ── MCP Protocol (stdio) ──────────────────────────────────────────────────────

async def main():
    while True:
        line = await asyncio.get_event_loop().run_in_executor(None, sys.stdin.readline)
        if not line:
            break
        try:
            msg = json.loads(line.strip())
        except json.JSONDecodeError:
            continue

        method = msg.get("method")
        msg_id = msg.get("id")

        if method == "initialize":
            resp = {
                "jsonrpc": "2.0", "id": msg_id,
                "result": {
                    "protocolVersion": "2024-11-05",
                    "capabilities": {"tools": {}},
                    "serverInfo": {"name": "unreal-mcp", "version": "1.0.0"}
                }
            }
        elif method == "tools/list":
            resp = {"jsonrpc": "2.0", "id": msg_id, "result": {"tools": TOOLS}}
        elif method == "tools/call":
            name = msg["params"]["name"]
            args = msg["params"].get("arguments", {})
            try:
                result = await handle_tool(name, args)
                resp = {"jsonrpc": "2.0", "id": msg_id,
                        "result": {"content": [{"type": "text", "text": str(result)}]}}
            except Exception as e:
                resp = {"jsonrpc": "2.0", "id": msg_id,
                        "error": {"code": -32000, "message": str(e)}}
        else:
            resp = {"jsonrpc": "2.0", "id": msg_id,
                    "error": {"code": -32601, "message": f"Method not found: {method}"}}

        print(json.dumps(resp), flush=True)


if __name__ == "__main__":
    asyncio.run(main())
