---
name: gameobject-component-list-all
description: List C# class names extended from UnityEngine.Component. Use this to find component type names for 'gameobject-component-add' tool. Results are paginated to avoid overwhelming responses.
---

# GameObject / Component / List All

List C# class names extended from UnityEngine.Component. Use this to find component type names for 'gameobject-component-add' tool. Results are paginated to avoid overwhelming responses.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/gameobject-component-list-all \
  -H "Content-Type: application/json" \
  -d '{
  "search": "string_value",
  "page": 0,
  "pageSize": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/gameobject-component-list-all \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "search": "string_value",
  "page": 0,
  "pageSize": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `search` | `string` | No | Substring for searching components. Could be empty. |
| `page` | `integer` | No | Page number (0-based). Default is 0. |
| `pageSize` | `integer` | No | Number of items per page. Default is 5. Max is 500. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "search": {
      "type": "string",
      "description": "Substring for searching components. Could be empty."
    },
    "page": {
      "type": "integer",
      "description": "Page number (0-based). Default is 0."
    },
    "pageSize": {
      "type": "integer",
      "description": "Number of items per page. Default is 5. Max is 500."
    }
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject\u002BComponentListResult"
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject\u002BComponentListResult": {
      "type": "object",
      "properties": {
        "Items": {
          "$ref": "#/$defs/System.String[]",
          "description": "Array of component type names for the current page."
        },
        "Page": {
          "type": "integer",
          "description": "Current page number (0-based)."
        },
        "PageSize": {
          "type": "integer",
          "description": "Number of items per page."
        },
        "TotalCount": {
          "type": "integer",
          "description": "Total number of matching components."
        },
        "TotalPages": {
          "type": "integer",
          "description": "Total number of pages available."
        }
      },
      "required": [
        "Page",
        "PageSize",
        "TotalCount",
        "TotalPages"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

