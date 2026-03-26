---
name: assets-find
description: Search the asset database using the search filter string. Allows you to search for Assets. The string argument can provide names, labels or types (classnames).
---

# Assets / Find

Search the asset database using the search filter string. Allows you to search for Assets. The string argument can provide names, labels or types (classnames).

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/assets-find \
  -H "Content-Type: application/json" \
  -d '{
  "filter": "string_value",
  "searchInFolders": "string_value",
  "maxResults": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/assets-find \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "filter": "string_value",
  "searchInFolders": "string_value",
  "maxResults": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filter` | `string` | No | The filter string can contain search data. Could be empty. Name: Filter assets by their filename (without extension). Words separated by whitespace are treated as separate name searches. Labels (l:): Assets can have labels attached to them. Use 'l:' before each label. Types (t:): Find assets based on explicitly identified types. Use 't:' keyword. Available types: AnimationClip, AudioClip, AudioMixer, ComputeShader, Font, GUISkin, Material, Mesh, Model, PhysicMaterial, Prefab, Scene, Script, Shader, Sprite, Texture, VideoClip, VisualEffectAsset, VisualEffectSubgraph. AssetBundles (b:): Find assets which are part of an Asset bundle. Area (a:): Find assets in a specific area. Valid values are 'all', 'assets', and 'packages'. Globbing (glob:): Use globbing to match specific rules. Note: Searching is case insensitive. |
| `searchInFolders` | `any` | No | The folders where the search will start. If null, the search will be performed in all folders. |
| `maxResults` | `integer` | No | Maximum number of assets to return. If the number of found assets exceeds this limit, the result will be truncated. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filter": {
      "type": "string",
      "description": "The filter string can contain search data. Could be empty. Name: Filter assets by their filename (without extension). Words separated by whitespace are treated as separate name searches. Labels (l:): Assets can have labels attached to them. Use \u0027l:\u0027 before each label. Types (t:): Find assets based on explicitly identified types. Use \u0027t:\u0027 keyword. Available types: AnimationClip, AudioClip, AudioMixer, ComputeShader, Font, GUISkin, Material, Mesh, Model, PhysicMaterial, Prefab, Scene, Script, Shader, Sprite, Texture, VideoClip, VisualEffectAsset, VisualEffectSubgraph. AssetBundles (b:): Find assets which are part of an Asset bundle. Area (a:): Find assets in a specific area. Valid values are \u0027all\u0027, \u0027assets\u0027, and \u0027packages\u0027. Globbing (glob:): Use globbing to match specific rules. Note: Searching is case insensitive."
    },
    "searchInFolders": {
      "$ref": "#/$defs/System.String[]",
      "description": "The folders where the search will start. If null, the search will be performed in all folders."
    },
    "maxResults": {
      "type": "integer",
      "description": "Maximum number of assets to return. If the number of found assets exceeds this limit, the result will be truncated."
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
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
      "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef\u003E"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is \u00270\u0027 and \u0027assetPath\u0027 and \u0027assetGuid\u0027 is not provided, empty or null, then it will be used as \u0027null\u0027."
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
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    },
    "System.Type": {
      "type": "string"
    },
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
        "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

