---
name: console-get-logs
description: Retrieves Unity Editor logs. Useful for debugging and monitoring Unity Editor activity.
---

# Console / Get Logs

Retrieves Unity Editor logs. Useful for debugging and monitoring Unity Editor activity.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/console-get-logs \
  -H "Content-Type: application/json" \
  -d '{
  "maxEntries": 0,
  "logTypeFilter": "string_value",
  "includeStackTrace": false,
  "lastMinutes": 0
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/console-get-logs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "maxEntries": 0,
  "logTypeFilter": "string_value",
  "includeStackTrace": false,
  "lastMinutes": 0
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `maxEntries` | `integer` | No | Maximum number of log entries to return. Minimum: 1. Default: 100 |
| `logTypeFilter` | `any` | No | Filter by log type. 'null' means All. |
| `includeStackTrace` | `boolean` | No | Include stack traces in the output. Default: false |
| `lastMinutes` | `integer` | No | Return logs from the last N minutes. If 0, returns all available logs. Default: 0 |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "maxEntries": {
      "type": "integer",
      "description": "Maximum number of log entries to return. Minimum: 1. Default: 100"
    },
    "logTypeFilter": {
      "$ref": "#/$defs/UnityEngine.LogType",
      "description": "Filter by log type. \u0027null\u0027 means All."
    },
    "includeStackTrace": {
      "type": "boolean",
      "description": "Include stack traces in the output. Default: false"
    },
    "lastMinutes": {
      "type": "integer",
      "description": "Return logs from the last N minutes. If 0, returns all available logs. Default: 0"
    }
  },
  "$defs": {
    "UnityEngine.LogType": {
      "type": "string",
      "enum": [
        "Error",
        "Assert",
        "Warning",
        "Log",
        "Exception"
      ]
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry[]"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.LogEntry": {
      "type": "object",
      "properties": {
        "LogType": {
          "type": "string",
          "enum": [
            "Error",
            "Assert",
            "Warning",
            "Log",
            "Exception"
          ]
        },
        "Message": {
          "type": "string"
        },
        "Timestamp": {
          "type": "string",
          "format": "date-time"
        },
        "StackTrace": {
          "type": "string"
        }
      },
      "required": [
        "LogType",
        "Timestamp"
      ]
    },
    "com.IvanMurzak.Unity.MCP.LogEntry[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

