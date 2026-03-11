---
name: assets-move
description: Move the assets at paths in the project. Should be used for asset rename. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before moving.
---

# Assets / Move

Move the assets at paths in the project. Should be used for asset rename. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before moving.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/assets-move \
  -H "Content-Type: application/json" \
  -d '{
  "sourcePaths": "string_value",
  "destinationPaths": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/assets-move \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "sourcePaths": "string_value",
  "destinationPaths": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourcePaths` | `any` | Yes | The paths of the assets to move. |
| `destinationPaths` | `any` | Yes | The paths of moved assets. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sourcePaths": {
      "$ref": "#/$defs/System.String[]",
      "description": "The paths of the assets to move."
    },
    "destinationPaths": {
      "$ref": "#/$defs/System.String[]",
      "description": "The paths of moved assets."
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
    "sourcePaths",
    "destinationPaths"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BMoveAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List\u003CSystem.String\u003E": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BMoveAssetsResponse": {
      "type": "object",
      "properties": {
        "MovedPaths": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of destination paths of successfully moved assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of errors encountered during move operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

