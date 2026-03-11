---
name: scene-open
description: Open scene from the project asset file. Use 'assets-find' tool to find the scene asset first.
---

# Scene / Open

Open scene from the project asset file. Use 'assets-find' tool to find the scene asset first.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/scene-open \
  -H "Content-Type: application/json" \
  -d '{
  "sceneRef": "string_value",
  "loadSceneMode": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/scene-open \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "sceneRef": "string_value",
  "loadSceneMode": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sceneRef` | `any` | Yes | Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders. |
| `loadSceneMode` | `string` | No | Open scene mode. Single: closes the current scenes and opens a new one. Additive: keeps the current scene and opens additional one. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sceneRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    },
    "loadSceneMode": {
      "type": "string",
      "enum": [
        "Single",
        "Additive",
        "AdditiveWithoutLoading"
      ],
      "description": "Open scene mode. Single: closes the current scenes and opens a new one. Additive: keeps the current scene and opens additional one."
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
    "sceneRef"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow[]"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string"
        },
        "IsLoaded": {
          "type": "boolean"
        },
        "IsDirty": {
          "type": "boolean"
        },
        "IsSubScene": {
          "type": "boolean"
        },
        "IsValidScene": {
          "type": "boolean",
          "description": "Whether this is a valid Scene. A Scene may be invalid if, for example, you tried to open a Scene that does not exist. In this case, the Scene returned from EditorSceneManager.OpenScene would return False for IsValid."
        },
        "RootCount": {
          "type": "integer"
        },
        "path": {
          "type": "string",
          "description": "Path to the Scene within the project. Starts with \u0027Assets/\u0027"
        },
        "buildIndex": {
          "type": "integer",
          "description": "Build index of the Scene in the Build Settings."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is \u00270\u0027, then it will be used as \u0027null\u0027."
        }
      },
      "required": [
        "IsLoaded",
        "IsDirty",
        "IsSubScene",
        "IsValidScene",
        "RootCount",
        "buildIndex",
        "instanceID"
      ],
      "description": "Scene reference. Used to find a Scene."
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow",
        "description": "Scene reference. Used to find a Scene."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

