---
name: assets-prefab-save
description: Save a prefab. Use it when you are in prefab editing mode in Unity Editor. Use 'assets-prefab-open' tool to open a prefab first.
---

# Assets / Prefab / Save

Save a prefab. Use it when you are in prefab editing mode in Unity Editor. Use 'assets-prefab-open' tool to open a prefab first.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/assets-prefab-save \
  -H "Content-Type: application/json" \
  -d '{}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/assets-prefab-save \
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
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
    }
  },
  "required": [
    "result"
  ]
}
```

