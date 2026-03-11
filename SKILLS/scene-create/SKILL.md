---
name: scene-create
description: Create new scene in the project assets. Use 'scene-list-opened' tool to list all opened scenes after creation.
---

# Scene / Create

Create new scene in the project assets. Use 'scene-list-opened' tool to list all opened scenes after creation.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/scene-create \
  -H "Content-Type: application/json" \
  -d '{
  "path": "string_value",
  "newSceneSetup": "string_value",
  "newSceneMode": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/scene-create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "path": "string_value",
  "newSceneSetup": "string_value",
  "newSceneMode": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | `string` | Yes | Path to the scene file. Should end with ".unity" extension. |
| `newSceneSetup` | `any` | No |  |
| `newSceneMode` | `any` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "path": {
      "type": "string",
      "description": "Path to the scene file. Should end with \u0022.unity\u0022 extension."
    },
    "newSceneSetup": {
      "$ref": "#/$defs/UnityEditor.SceneManagement.NewSceneSetup"
    },
    "newSceneMode": {
      "$ref": "#/$defs/UnityEditor.SceneManagement.NewSceneMode"
    }
  },
  "$defs": {
    "UnityEditor.SceneManagement.NewSceneSetup": {
      "type": "string",
      "enum": [
        "EmptyScene",
        "DefaultGameObjects"
      ]
    },
    "UnityEditor.SceneManagement.NewSceneMode": {
      "type": "string",
      "enum": [
        "Single",
        "Additive"
      ]
    }
  },
  "required": [
    "path"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow",
      "description": "Scene reference. Used to find a Scene."
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
    }
  },
  "required": [
    "result"
  ]
}
```

