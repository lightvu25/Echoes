---
name: gameobject-create
description: Create a new GameObject in opened Prefab or in a Scene. If needed - provide proper 'position', 'rotation' and 'scale' to reduce amount of operations.
---

# GameObject / Create

Create a new GameObject in opened Prefab or in a Scene. If needed - provide proper 'position', 'rotation' and 'scale' to reduce amount of operations.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/gameobject-create \
  -H "Content-Type: application/json" \
  -d '{
  "name": "string_value",
  "parentGameObjectRef": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "scale": "string_value",
  "isLocalSpace": false,
  "primitiveType": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/gameobject-create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "name": "string_value",
  "parentGameObjectRef": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "scale": "string_value",
  "isLocalSpace": false,
  "primitiveType": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | `string` | Yes | Name of the new GameObject. |
| `parentGameObjectRef` | `any` | No | Parent GameObject reference. If not provided, the GameObject will be created at the root of the scene or prefab. |
| `position` | `any` | No | Transform position of the GameObject. |
| `rotation` | `any` | No | Transform rotation of the GameObject. Euler angles in degrees. |
| `scale` | `any` | No | Transform scale of the GameObject. |
| `isLocalSpace` | `boolean` | No | World or Local space of transform. |
| `primitiveType` | `any` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "Name of the new GameObject."
    },
    "parentGameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Parent GameObject reference. If not provided, the GameObject will be created at the root of the scene or prefab."
    },
    "position": {
      "$ref": "#/$defs/UnityEngine.Vector3",
      "description": "Transform position of the GameObject."
    },
    "rotation": {
      "$ref": "#/$defs/UnityEngine.Vector3",
      "description": "Transform rotation of the GameObject. Euler angles in degrees."
    },
    "scale": {
      "$ref": "#/$defs/UnityEngine.Vector3",
      "description": "Transform scale of the GameObject."
    },
    "isLocalSpace": {
      "type": "boolean",
      "description": "World or Local space of transform."
    },
    "primitiveType": {
      "$ref": "#/$defs/UnityEngine.PrimitiveType"
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
    },
    "UnityEngine.Vector3": {
      "type": "object",
      "properties": {
        "x": {
          "type": "number"
        },
        "y": {
          "type": "number"
        },
        "z": {
          "type": "number"
        }
      },
      "required": [
        "x",
        "y",
        "z"
      ],
      "additionalProperties": false
    },
    "UnityEngine.PrimitiveType": {
      "type": "string",
      "enum": [
        "Sphere",
        "Capsule",
        "Cylinder",
        "Cube",
        "Plane",
        "Quad"
      ]
    }
  },
  "required": [
    "name"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
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
    "result"
  ]
}
```

