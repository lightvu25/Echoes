---
name: scene-list-opened
description: Returns the list of currently opened scenes in Unity Editor. Use 'scene-get-data' tool to get detailed information about a specific scene.
---

# Scene / List Opened

Returns the list of currently opened scenes in Unity Editor. Use 'scene-get-data' tool to get detailed information about a specific scene.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/scene-list-opened \
  -H "Content-Type: application/json" \
  -d '{}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/scene-list-opened \
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

