---
name: assets-prefab-create
description: Create a prefab from a GameObject in the current active scene. The prefab will be saved in the project assets at the specified path. Use 'gameobject-find' tool to find the target GameObject first.
---

# Assets / Prefab / Create

Create a prefab from a GameObject in the current active scene. The prefab will be saved in the project assets at the specified path. Use 'gameobject-find' tool to find the target GameObject first.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/assets-prefab-create \
  -H "Content-Type: application/json" \
  -d '{
  "prefabAssetPath": "string_value",
  "gameObjectRef": "string_value",
  "replaceGameObjectWithPrefab": false
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/assets-prefab-create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "prefabAssetPath": "string_value",
  "gameObjectRef": "string_value",
  "replaceGameObjectWithPrefab": false
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | Yes | Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'. |
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `replaceGameObjectWithPrefab` | `boolean` | No | If true, the prefab will replace the GameObject in the scene. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "prefabAssetPath": {
      "type": "string",
      "description": "Prefab asset path. Should be in the format \u0027Assets/Path/To/Prefab.prefab\u0027."
    },
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "replaceGameObjectWithPrefab": {
      "type": "boolean",
      "description": "If true, the prefab will replace the GameObject in the scene."
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If it is \u00270\u0027 and \u0027path\u0027, \u0027name\u0027, \u0027assetPath\u0027 and \u0027assetGuid\u0027 is not provided, empty or null, then it will be used as \u0027null\u0027. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample \u0027character/hand/finger/particle\u0027. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
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
      "description": "Find GameObject in opened Prefab or in the active Scene."
    }
  },
  "required": [
    "prefabAssetPath",
    "gameObjectRef"
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

