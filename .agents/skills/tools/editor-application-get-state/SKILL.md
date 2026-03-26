---
name: editor-application-get-state
description: "Returns available information about 'UnityEditor.EditorApplication'. Use it to get information about the current state of the Unity Editor application. Such as: playmode, paused state, compilation state, etc."
---

# Editor / Application / Get State

Returns available information about 'UnityEditor.EditorApplication'. Use it to get information about the current state of the Unity Editor application. Such as: playmode, paused state, compilation state, etc.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/editor-application-get-state \
  -H "Content-Type: application/json" \
  -d '{
  "nothing": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/editor-application-get-state \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "nothing": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nothing` | `string` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "nothing": {
      "type": "string"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Editor\u002BEditorStatsData",
      "description": "Available information about \u0027UnityEditor.EditorApplication\u0027."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Editor\u002BEditorStatsData": {
      "type": "object",
      "properties": {
        "IsPlaying": {
          "type": "boolean",
          "description": "Whether the Editor is in Play mode."
        },
        "IsPaused": {
          "type": "boolean",
          "description": "Whether the Editor is paused."
        },
        "IsCompiling": {
          "type": "boolean",
          "description": "Is editor currently compiling scripts? (Read Only)"
        },
        "IsPlayingOrWillChangePlaymode": {
          "type": "boolean",
          "description": "Editor application state which is true only when the Editor is currently in or about to enter Play mode. (Read Only)"
        },
        "IsUpdating": {
          "type": "boolean",
          "description": "True if the Editor is currently refreshing the AssetDatabase. (Read Only)"
        },
        "ApplicationContentsPath": {
          "type": "string",
          "description": "Path to the Unity editor contents folder. (Read Only)"
        },
        "ApplicationPath": {
          "type": "string",
          "description": "Gets the path to the Unity Editor application. (Read Only)"
        },
        "TimeSinceStartup": {
          "type": "number",
          "description": "The time since the editor was started. (Read Only)"
        }
      },
      "required": [
        "IsPlaying",
        "IsPaused",
        "IsCompiling",
        "IsPlayingOrWillChangePlaymode",
        "IsUpdating",
        "TimeSinceStartup"
      ],
      "description": "Available information about \u0027UnityEditor.EditorApplication\u0027."
    }
  },
  "required": [
    "result"
  ]
}
```

