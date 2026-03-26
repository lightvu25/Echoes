---
name: package-list
description: List all packages installed in the Unity project (UPM packages). Returns information about each installed package including name, version, source, and description. Use this to check which packages are currently installed before adding or removing packages.
---

# Package Manager / List Installed

List all packages installed in the Unity project (UPM packages). Returns information about each installed package including name, version, source, and description. Use this to check which packages are currently installed before adding or removing packages.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/package-list \
  -H "Content-Type: application/json" \
  -d '{
  "sourceFilter": "string_value",
  "nameFilter": "string_value",
  "directDependenciesOnly": false
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/package-list \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "sourceFilter": "string_value",
  "nameFilter": "string_value",
  "directDependenciesOnly": false
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourceFilter` | `string` | No | Filter packages by source. |
| `nameFilter` | `string` | No | Filter packages by name, display name, or description (case-insensitive). Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring. |
| `directDependenciesOnly` | `boolean` | No | Include only direct dependencies (packages in manifest.json). If false, includes all resolved packages. Default: false |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sourceFilter": {
      "type": "string",
      "enum": [
        "All",
        "Registry",
        "Embedded",
        "Local",
        "Git",
        "BuiltIn",
        "LocalTarball"
      ],
      "description": "Filter packages by source."
    },
    "nameFilter": {
      "type": "string",
      "description": "Filter packages by name, display name, or description (case-insensitive). Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring."
    },
    "directDependenciesOnly": {
      "type": "boolean",
      "description": "Include only direct dependencies (packages in manifest.json). If false, includes all resolved packages. Default: false"
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
      "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageData\u003E"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageData": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string",
          "description": "The official Unity name of the package used as the package ID."
        },
        "DisplayName": {
          "type": "string",
          "description": "The display name of the package."
        },
        "Version": {
          "type": "string",
          "description": "The version of the package."
        },
        "Description": {
          "type": "string",
          "description": "A brief description of the package."
        },
        "Source": {
          "type": "string",
          "description": "The source of the package (Registry, Embedded, Local, Git, etc.)."
        },
        "Category": {
          "type": "string",
          "description": "The category of the package."
        }
      },
      "description": "Package information returned from package list operation."
    },
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageData\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageData",
        "description": "Package information returned from package list operation."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

