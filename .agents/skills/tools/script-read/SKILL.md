---
name: script-read
description: Reads the content of a script file and returns it as a string. Use 'script-update-or-create' tool to update or create script files.
---

# Script / Read

Reads the content of a script file and returns it as a string. Use 'script-update-or-create' tool to update or create script files.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/script-read \
  -H "Content-Type: application/json" \
  -d '{
  "filePath": "string_value",
  "lineFrom": 0,
  "lineTo": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/script-read \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "filePath": "string_value",
  "lineFrom": 0,
  "lineTo": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `lineFrom` | `integer` | No | The line number to start reading from (1-based). |
| `lineTo` | `integer` | No | The line number to stop reading at (1-based, -1 for all lines). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string",
      "description": "The path to the file. Sample: \u0022Assets/Scripts/MyScript.cs\u0022."
    },
    "lineFrom": {
      "type": "integer",
      "description": "The line number to start reading from (1-based)."
    },
    "lineTo": {
      "type": "integer",
      "description": "The line number to stop reading at (1-based, -1 for all lines)."
    }
  },
  "required": [
    "filePath"
  ]
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

