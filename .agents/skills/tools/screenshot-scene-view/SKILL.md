---
name: screenshot-scene-view
description: Captures a screenshot from the Unity Editor Scene View and returns it as an image. Returns the image directly for visual inspection by the LLM.
---

# Screenshot / Scene View

Captures a screenshot from the Unity Editor Scene View and returns it as an image. Returns the image directly for visual inspection by the LLM.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/screenshot-scene-view \
  -H "Content-Type: application/json" \
  -d '{
  "width": 0,
  "height": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/screenshot-scene-view \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "width": 0,
  "height": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `width` | `integer` | No | Width of the screenshot in pixels. |
| `height` | `integer` | No | Height of the screenshot in pixels. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "width": {
      "type": "integer",
      "description": "Width of the screenshot in pixels."
    },
    "height": {
      "type": "integer",
      "description": "Height of the screenshot in pixels."
    }
  }
}
```

## Output

This tool does not return structured output.

