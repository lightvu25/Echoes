---
name: screenshot-game-view
description: Captures a screenshot from the Unity Editor Game View and returns it as an image. Reads the Game View's own render texture directly via the Unity Editor API. The image size matches the current Game View resolution. Returns the image directly for visual inspection by the LLM.
---

# Screenshot / Game View

Captures a screenshot from the Unity Editor Game View and returns it as an image. Reads the Game View's own render texture directly via the Unity Editor API. The image size matches the current Game View resolution. Returns the image directly for visual inspection by the LLM.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/screenshot-game-view \
  -H "Content-Type: application/json" \
  -d '{}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/screenshot-game-view \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

This tool takes no input parameters.

### Input JSON Schema

```json
{
  "type": "object"
}
```

## Output

This tool does not return structured output.

