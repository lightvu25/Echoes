---
name: type-get-json-schema
description: Generates a JSON Schema for a given C# type name using reflection. Supports primitives, enums, arrays, generic collections, dictionaries, and complex objects. The type must be present in any loaded assembly. Use the full type name (e.g. 'UnityEngine.Vector3') for best results.
---

# Type / Get Json Schema

Generates a JSON Schema for a given C# type name using reflection. Supports primitives, enums, arrays, generic collections, dictionaries, and complex objects. The type must be present in any loaded assembly. Use the full type name (e.g. 'UnityEngine.Vector3') for best results.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/type-get-json-schema \
  -H "Content-Type: application/json" \
  -d '{
  "typeName": "string_value",
  "descriptionMode": "string_value",
  "propertyDescriptionMode": "string_value",
  "includeNestedTypes": false,
  "writeIndented": false
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/type-get-json-schema \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "typeName": "string_value",
  "descriptionMode": "string_value",
  "propertyDescriptionMode": "string_value",
  "includeNestedTypes": false,
  "writeIndented": false
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `typeName` | `string` | Yes | Full C# type name to generate the schema for. Examples: 'System.String', 'UnityEngine.Vector3', 'System.Collections.Generic.List<System.Int32>'. Simple names like 'Vector3' are also accepted when unambiguous. |
| `descriptionMode` | `string` | No | Controls the type-level 'description' field. Include: keep on the target type only. IncludeRecursively: keep on the target type and inside $defs entries. Ignore: strip all type-level descriptions. Default: Ignore. |
| `propertyDescriptionMode` | `string` | No | Controls 'description' fields on properties, fields, and array items. Include: keep on the target type's own properties/items only. IncludeRecursively: keep on all properties/items including those inside $defs entries. Ignore: strip all property/item descriptions. Default: Ignore. |
| `includeNestedTypes` | `boolean` | No | When true, complex nested types are extracted into '$defs' and referenced via '$ref' instead of being inlined. Useful for large or recursive types. Default: false. |
| `writeIndented` | `boolean` | No | Whether to format the output JSON with indentation for readability. Default: false. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "typeName": {
      "type": "string",
      "description": "Full C# type name to generate the schema for. Examples: \u0027System.String\u0027, \u0027UnityEngine.Vector3\u0027, \u0027System.Collections.Generic.List\u003CSystem.Int32\u003E\u0027. Simple names like \u0027Vector3\u0027 are also accepted when unambiguous."
    },
    "descriptionMode": {
      "type": "string",
      "enum": [
        "Include",
        "IncludeRecursively",
        "Ignore"
      ],
      "description": "Controls the type-level \u0027description\u0027 field. Include: keep on the target type only. IncludeRecursively: keep on the target type and inside $defs entries. Ignore: strip all type-level descriptions. Default: Ignore."
    },
    "propertyDescriptionMode": {
      "type": "string",
      "enum": [
        "Include",
        "IncludeRecursively",
        "Ignore"
      ],
      "description": "Controls \u0027description\u0027 fields on properties, fields, and array items. Include: keep on the target type\u0027s own properties/items only. IncludeRecursively: keep on all properties/items including those inside $defs entries. Ignore: strip all property/item descriptions. Default: Ignore."
    },
    "includeNestedTypes": {
      "type": "boolean",
      "description": "When true, complex nested types are extracted into \u0027$defs\u0027 and referenced via \u0027$ref\u0027 instead of being inlined. Useful for large or recursive types. Default: false."
    },
    "writeIndented": {
      "type": "boolean",
      "description": "Whether to format the output JSON with indentation for readability. Default: false."
    }
  },
  "required": [
    "typeName"
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
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

