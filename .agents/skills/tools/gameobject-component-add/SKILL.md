---
name: gameobject-component-add
description: Add Component to GameObject in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObject first. Use 'gameobject-component-list-all' tool to find the component type names to add.
---

# GameObject / Component / Add

Add Component to GameObject in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObject first. Use 'gameobject-component-list-all' tool to find the component type names to add.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/gameobject-component-add \
  -H "Content-Type: application/json" \
  -d '{
  "componentNames": "string_value",
  "gameObjectRef": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/gameobject-component-add \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "componentNames": "string_value",
  "gameObjectRef": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `componentNames` | `any` | Yes | Full name of the Component. It should include full namespace path and the class name. |
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "componentNames": {
      "$ref": "#/$defs/System.String[]",
      "description": "Full name of the Component. It should include full namespace path and the class name."
    },
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
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
    "componentNames",
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject\u002BAddComponentResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow"
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "typeName": {
          "type": "string"
        },
        "isEnabled": {
          "type": "string",
          "enum": [
            "False",
            "True",
            "NA"
          ]
        }
      },
      "required": [
        "instanceID",
        "isEnabled"
      ]
    },
    "System.Collections.Generic.List\u003CSystem.String\u003E": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject\u002BAddComponentResponse": {
      "type": "object",
      "properties": {
        "AddedComponents": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow\u003E",
          "description": "List of successfully added components."
        },
        "Messages": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of success messages for added components."
        },
        "Warnings": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of warnings encountered during component addition."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of errors encountered during component addition."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

