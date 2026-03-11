---
name: gameobject-component-modify
description: Modify a specific Component on a GameObject in opened Prefab or in a Scene. Allows direct modification of component fields and properties without wrapping in GameObject structure. Use 'gameobject-component-get' first to inspect the component structure before modifying.
---

# GameObject / Component / Modify

Modify a specific Component on a GameObject in opened Prefab or in a Scene. Allows direct modification of component fields and properties without wrapping in GameObject structure. Use 'gameobject-component-get' first to inspect the component structure before modifying.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/gameobject-component-modify \
  -H "Content-Type: application/json" \
  -d '{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "componentDiff": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/gameobject-component-modify \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "componentDiff": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `componentRef` | `any` | Yes | Component reference. Used to find a Component at GameObject. |
| `componentDiff` | `any` | Yes | The component data to apply. Should contain 'fields' and/or 'props' with the values to modify.
Only include the fields/properties you want to change.
Any unknown or invalid fields and properties will be reported in the response. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "componentRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef",
      "description": "Component reference. Used to find a Component at GameObject."
    },
    "componentDiff": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
      "description": "The component data to apply. Should contain \u0027fields\u0027 and/or \u0027props\u0027 with the values to modify.\nOnly include the fields/properties you want to change.\nAny unknown or invalid fields and properties will be reported in the response."
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
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component \u0027index\u0027 attached to a gameObject. The first index is \u00270\u0027 and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample \u0027UnityEngine.Transform\u0027. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is \u00270\u0027, then it will be used as \u0027null\u0027."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
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
    }
  },
  "required": [
    "gameObjectRef",
    "componentRef",
    "componentDiff"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject\u002BModifyComponentResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component \u0027index\u0027 attached to a gameObject. The first index is \u00270\u0027 and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample \u0027UnityEngine.Transform\u0027. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is \u00270\u0027, then it will be used as \u0027null\u0027."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
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
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject\u002BModifyComponentResponse": {
      "type": "object",
      "properties": {
        "Success": {
          "type": "boolean",
          "description": "Whether the modification was successful."
        },
        "Reference": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef",
          "description": "Reference to the modified component."
        },
        "Index": {
          "type": "integer",
          "description": "Index of the component in the GameObject\u0027s component list."
        },
        "Component": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow",
          "description": "Updated component information after modification."
        },
        "Logs": {
          "$ref": "#/$defs/System.String[]",
          "description": "Log of modifications made and any warnings/errors encountered."
        }
      },
      "required": [
        "Success",
        "Index"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

