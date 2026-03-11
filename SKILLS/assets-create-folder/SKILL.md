---
name: assets-create-folder
description: Creates a new folder in the specified parent folder. The parent folder string must start with the 'Assets' folder, and all folders within the parent folder string must already exist. For example, when specifying 'Assets/ParentFolder1/ParentFolder2/', the new folder will be created in 'ParentFolder2' only if ParentFolder1 and ParentFolder2 already exist. Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end. Returns the GUID of the newly created folder, if successful.
---

# Assets / Create Folder

Creates a new folder in the specified parent folder. The parent folder string must start with the 'Assets' folder, and all folders within the parent folder string must already exist. For example, when specifying 'Assets/ParentFolder1/ParentFolder2/', the new folder will be created in 'ParentFolder2' only if ParentFolder1 and ParentFolder2 already exist. Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end. Returns the GUID of the newly created folder, if successful.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/assets-create-folder \
  -H "Content-Type: application/json" \
  -d '{
  "inputs": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/assets-create-folder \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "inputs": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `inputs` | `any` | Yes | The paths for the folders to create. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "inputs": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCreateFolderInput[]",
      "description": "The paths for the folders to create."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCreateFolderInput": {
      "type": "object",
      "properties": {
        "ParentFolderPath": {
          "type": "string",
          "description": "The parent folder path where the new folder will be created."
        },
        "NewFolderName": {
          "type": "string",
          "description": "The name of the new folder to create."
        }
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCreateFolderInput[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCreateFolderInput"
      }
    }
  },
  "required": [
    "inputs"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCreateFolderResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List\u003CSystem.String\u003E": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets\u002BCreateFolderResponse": {
      "type": "object",
      "properties": {
        "CreatedFolderGuids": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of GUIDs of created folders."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "List of errors encountered during folder creation."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

