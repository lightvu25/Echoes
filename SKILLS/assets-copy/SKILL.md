---
name: assets-copy
description: Copy the asset at path and stores it at newPath. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before copying.
---

# Assets / Copy

Copy the asset at path and stores it at newPath. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before copying.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/assets-copy \
  -H "Content-Type: application/json" \
  -d '{
  "sourcePaths": "string_value",
  "destinationPaths": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/assets-copy \
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
| `sourcePaths` | `any` | Yes | The paths of the asset to copy. |
| `destinationPaths` | `any` | Yes | The paths to store the copied asset. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sourcePaths": {
      "$ref": "#/$defs/System.String[]",
      "description": "The paths of the asset to copy."
    },
    "destinationPaths": {
      "$ref": "#/$defs/System.String[]",
      "description": "The paths to store the copied asset."
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCopyAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
        "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is \u00270\u0027 and \u0027assetPath\u0027 and \u0027assetGuid\u0027 is not provided, empty or null, then it will be used as \u0027null\u0027."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with \u0027Assets/\u0027"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    },
    "System.Type": {
      "type": "string"
    },
    "System.Collections.Generic.List\u003CSystem.String\u003E": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCopyAssetsResponse": {
      "type": "object",
      "properties": {
        "CopiedAssets": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef\u003E",
          "description": "List of copied assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of errors encountered during copy operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

