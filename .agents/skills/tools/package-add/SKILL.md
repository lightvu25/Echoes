---
name: package-add
description: "Install a package from the Unity Package Manager registry, Git URL, or local path. This operation modifies the project's manifest.json and triggers package resolution. Note: Package installation may trigger a domain reload. The result will be sent after the reload completes. Use 'package-search' tool to search for packages and 'package-list' to list installed packages."
---

# Package Manager / Add

Install a package from the Unity Package Manager registry, Git URL, or local path. This operation modifies the project's manifest.json and triggers package resolution. Note: Package installation may trigger a domain reload. The result will be sent after the reload completes. Use 'package-search' tool to search for packages and 'package-list' to list installed packages.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:58347/api/tools/package-add \
  -H "Content-Type: application/json" \
  -d '{
  "packageId": "string_value"
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:58347/api/tools/package-add \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "packageId": "string_value"
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `packageId` | `string` | Yes | The package ID to install. Formats: Package ID 'com.unity.textmeshpro' (installs latest compatible version), Package ID with version 'com.unity.textmeshpro@3.0.6', Git URL 'https://github.com/user/repo.git', Git URL with branch/tag 'https://github.com/user/repo.git#v1.0.0', Local path 'file:../MyPackage'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "packageId": {
      "type": "string",
      "description": "The package ID to install. Formats: Package ID \u0027com.unity.textmeshpro\u0027 (installs latest compatible version), Package ID with version \u0027com.unity.textmeshpro@3.0.6\u0027, Git URL \u0027https://github.com/user/repo.git\u0027, Git URL with branch/tag \u0027https://github.com/user/repo.git#v1.0.0\u0027, Local path \u0027file:../MyPackage\u0027."
    }
  },
  "required": [
    "packageId"
  ]
}
```

## Output

This tool does not return structured output.

