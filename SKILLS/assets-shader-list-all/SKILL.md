---
name: assets-shader-list-all
description: List all available shaders in the project assets and packages. Returns their names. Use this to find a shader name for 'assets-material-create' tool.
---

# Assets / List Shaders

List all available shaders in the project assets and packages. Returns their names. Use this to find a shader name for 'assets-material-create' tool.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/assets-shader-list-all \
  -H "Content-Type: application/json" \
  -d '{}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/assets-shader-list-all \
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

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/System.String[]"
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

