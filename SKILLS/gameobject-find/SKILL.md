---
name: gameobject-find
description: Finds specific GameObject by provided information in opened Prefab or in a Scene. First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene. If no opened Prefab it looks into current active scene. Returns GameObject information and its children. Also, it returns Components preview just for the target GameObject.
---

# GameObject / Find

Finds specific GameObject by provided information in opened Prefab or in a Scene. First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene. If no opened Prefab it looks into current active scene. Returns GameObject information and its children. Also, it returns Components preview just for the target GameObject.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/gameobject-find \
  -H "Content-Type: application/json" \
  -d '{
  "gameObjectRef": "string_value",
  "includeData": false,
  "includeComponents": false,
  "includeBounds": false,
  "includeHierarchy": false,
  "hierarchyDepth": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/gameobject-find \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "gameObjectRef": "string_value",
  "includeData": false,
  "includeComponents": false,
  "includeBounds": false,
  "includeHierarchy": false,
  "hierarchyDepth": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `includeData` | `boolean` | No | Include editable GameObject data (tag, layer, etc). |
| `includeComponents` | `boolean` | No | Include attached components references. |
| `includeBounds` | `boolean` | No | Include 3D bounds of the GameObject. |
| `includeHierarchy` | `boolean` | No | Include hierarchy metadata. |
| `hierarchyDepth` | `integer` | No | Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "includeData": {
      "type": "boolean",
      "description": "Include editable GameObject data (tag, layer, etc)."
    },
    "includeComponents": {
      "type": "boolean",
      "description": "Include attached components references."
    },
    "includeBounds": {
      "type": "boolean",
      "description": "Include 3D bounds of the GameObject."
    },
    "includeHierarchy": {
      "type": "boolean",
      "description": "Include hierarchy metadata."
    },
    "hierarchyDepth": {
      "type": "integer",
      "description": "Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below."
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectData"
    }
  },
  "$defs": {
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
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: \u0027System.String\u0027, \u0027System.Int32\u0027, \u0027UnityEngine.Vector3\u0027, etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of \u0027SerializedMember\u0027."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of \u0027SerializedMember\u0027."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "UnityEngine.Bounds": {
      "type": "object",
      "properties": {
        "center": {
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
          ]
        },
        "size": {
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
          ]
        }
      },
      "required": [
        "center",
        "size"
      ],
      "additionalProperties": false
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "path": {
          "type": "string"
        },
        "name": {
          "type": "string"
        },
        "sceneName": {
          "type": "string"
        },
        "tag": {
          "type": "string"
        },
        "activeSelf": {
          "type": "boolean"
        },
        "activeInHierarchy": {
          "type": "boolean"
        },
        "children": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata\u003E"
        }
      },
      "required": [
        "instanceID",
        "activeSelf",
        "activeInHierarchy"
      ]
    },
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata"
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow[]": {
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
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectData": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
          "description": "Find GameObject in opened Prefab or in the active Scene."
        },
        "Data": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "GameObject editable data (tag, layer, etc)."
        },
        "Bounds": {
          "$ref": "#/$defs/UnityEngine.Bounds",
          "description": "Bounds of the GameObject."
        },
        "Hierarchy": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata",
          "description": "Hierarchy metadata of the GameObject."
        },
        "Components": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow[]",
          "description": "Attached components shallow data of the GameObject (Read-only, use Component modification tool for modification)."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

