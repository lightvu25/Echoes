---
name: scene-save
description: Save Opened scene to the asset file. Use 'scene-list-opened' tool to get the list of all opened scenes.
---

# Scene / Save

Save Opened scene to the asset file. Use 'scene-list-opened' tool to get the list of all opened scenes.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/scene-save \
  -H "Content-Type: application/json" \
  -d '{
  "openedSceneName": "string_value",
  "path": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/scene-save \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "openedSceneName": "string_value",
  "path": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `openedSceneName` | `string` | No | Name of the opened scene that should be saved. Could be empty if need to save the current active scene. |
| `path` | `string` | No | Path to the scene file. Should end with ".unity". If null or empty save to the existed scene asset file. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "openedSceneName": {
      "type": "string",
      "description": "Name of the opened scene that should be saved. Could be empty if need to save the current active scene."
    },
    "path": {
      "type": "string",
      "description": "Path to the scene file. Should end with \u0022.unity\u0022. If null or empty save to the existed scene asset file."
    }
  }
}
```

## Output

This tool does not return structured output.

