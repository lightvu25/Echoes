---
name: screenshot-camera
description: Captures a screenshot from a camera and returns it as an image. If no camera is specified, uses the Main Camera. Returns the image directly for visual inspection by the LLM.
---

# Screenshot / Camera

Captures a screenshot from a camera and returns it as an image. If no camera is specified, uses the Main Camera. Returns the image directly for visual inspection by the LLM.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/screenshot-camera \
  -H "Content-Type: application/json" \
  -d '{
  "cameraRef": "string_value",
  "width": 0,
  "height": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/screenshot-camera \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "cameraRef": "string_value",
  "width": 0,
  "height": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `cameraRef` | `any` | No | Reference to the camera GameObject. If not specified, uses the Main Camera. |
| `width` | `integer` | No | Width of the screenshot in pixels. |
| `height` | `integer` | No | Height of the screenshot in pixels. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "cameraRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Reference to the camera GameObject. If not specified, uses the Main Camera."
    },
    "width": {
      "type": "integer",
      "description": "Width of the screenshot in pixels."
    },
    "height": {
      "type": "integer",
      "description": "Height of the screenshot in pixels."
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
  }
}
```

## Output

This tool does not return structured output.

