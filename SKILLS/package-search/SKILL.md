---
name: package-search
description: "Search for packages in both Unity Package Manager registry and installed packages. Use this to find packages by name before installing them. Returns available versions and installation status. Searches both the Unity registry and locally installed packages (including Git, local, and embedded sources). Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring. Note: Online mode fetches exact matches from live registry, then supplements with cached substring matches."
---

# Package Manager / Search

Search for packages in both Unity Package Manager registry and installed packages. Use this to find packages by name before installing them. Returns available versions and installation status. Searches both the Unity registry and locally installed packages (including Git, local, and embedded sources). Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring. Note: Online mode fetches exact matches from live registry, then supplements with cached substring matches.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/package-search \
  -H "Content-Type: application/json" \
  -d '{
  "query": "string_value",
  "maxResults": 0,
  "offlineMode": false
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/package-search \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "query": "string_value",
  "maxResults": 0,
  "offlineMode": false
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `query` | `string` | Yes | The package id, name, or description. Can be: Full package id 'com.unity.textmeshpro', Full package name 'TextMesh Pro', Partial name 'TextMesh' (will search in Unity registry and installed packages), Description keyword 'rendering' (searches in package descriptions). |
| `maxResults` | `integer` | No | Maximum number of results to return. Default: 10 |
| `offlineMode` | `boolean` | No | Whether to perform the search in offline mode (uses cached registry data only). Default: true. Set to false to fetch latest exact matches from Unity registry. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string",
      "description": "The package id, name, or description. Can be: Full package id \u0027com.unity.textmeshpro\u0027, Full package name \u0027TextMesh Pro\u0027, Partial name \u0027TextMesh\u0027 (will search in Unity registry and installed packages), Description keyword \u0027rendering\u0027 (searches in package descriptions)."
    },
    "maxResults": {
      "type": "integer",
      "description": "Maximum number of results to return. Default: 10"
    },
    "offlineMode": {
      "type": "boolean",
      "description": "Whether to perform the search in offline mode (uses cached registry data only). Default: true. Set to false to fetch latest exact matches from Unity registry."
    }
  },
  "required": [
    "query"
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
      "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageSearchResult\u003E"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageSearchResult": {
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
        "LatestVersion": {
          "type": "string",
          "description": "The latest version available in the registry."
        },
        "Description": {
          "type": "string",
          "description": "A brief description of the package."
        },
        "IsInstalled": {
          "type": "boolean",
          "description": "Whether this package is already installed in the project."
        },
        "InstalledVersion": {
          "type": "string",
          "description": "The currently installed version (if installed)."
        },
        "AvailableVersions": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003CSystem.String\u003E",
          "description": "Available versions of this package (up to 5 most recent)."
        }
      },
      "required": [
        "IsInstalled"
      ],
      "description": "Package search result with available versions."
    },
    "System.Collections.Generic.List\u003CSystem.String\u003E": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageSearchResult\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package\u002BPackageSearchResult",
        "description": "Package search result with available versions."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

