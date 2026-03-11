---
name: tests-run
description: Execute Unity tests and return detailed results. Supports filtering by test mode, assembly, namespace, class, and method. Recommended to use 'EditMode' for faster iteration during development.
---

# Tests / Run

Execute Unity tests and return detailed results. Supports filtering by test mode, assembly, namespace, class, and method. Recommended to use 'EditMode' for faster iteration during development.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://localhost:57228/api/tools/tests-run \
  -H "Content-Type: application/json" \
  -d '{
  "testMode": "string_value",
  "testAssembly": "string_value",
  "testNamespace": "string_value",
  "testClass": "string_value",
  "testMethod": "string_value",
  "includePassingTests": false,
  "includeMessages": false,
  "includeStacktrace": false,
  "includeLogs": false,
  "logType": "string_value",
  "includeLogsStacktrace": false
}'
```

#### With Authorization (if required)

```bash
curl -X POST http://localhost:57228/api/tools/tests-run \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "testMode": "string_value",
  "testAssembly": "string_value",
  "testNamespace": "string_value",
  "testClass": "string_value",
  "testMethod": "string_value",
  "includePassingTests": false,
  "includeMessages": false,
  "includeStacktrace": false,
  "includeLogs": false,
  "logType": "string_value",
  "includeLogsStacktrace": false
}'
```

> The token is stored in the file: `UserSettings/AI-Game-Developer-Config.json`
> Using the format: `"token": "YOUR_TOKEN"`

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `testMode` | `string` | No | Test mode to run. Options: 'EditMode', 'PlayMode'. Default: 'EditMode' |
| `testAssembly` | `string` | No | Specific test assembly name to run (optional). Example: 'Assembly-CSharp-Editor-testable' |
| `testNamespace` | `string` | No | Specific test namespace to run (optional). Example: 'MyTestNamespace' |
| `testClass` | `string` | No | Specific test class name to run (optional). Example: 'MyTestClass' |
| `testMethod` | `string` | No | Specific fully qualified test method to run (optional). Example: 'MyTestNamespace.FixtureName.TestName' |
| `includePassingTests` | `boolean` | No | Include details for all tests, both passing and failing (default: false). If you just need details for failing tests, set to false. |
| `includeMessages` | `boolean` | No | Include test result messages in the test results (default: true). If you just need pass/fail status, set to false. |
| `includeStacktrace` | `boolean` | No | Include stack traces in the test results (default: false). |
| `includeLogs` | `boolean` | No | Include console logs in the test results (default: false). |
| `logType` | `string` | No | Log type filter for console logs. Options: 'Log', 'Warning', 'Assert', 'Error', 'Exception'. (default: 'Warning') |
| `includeLogsStacktrace` | `boolean` | No | Include stack traces for console logs in the test results (default: false). This is huge amount of data, use only if really needed. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "testMode": {
      "type": "string",
      "enum": [
        "EditMode",
        "PlayMode"
      ],
      "description": "Test mode to run. Options: \u0027EditMode\u0027, \u0027PlayMode\u0027. Default: \u0027EditMode\u0027"
    },
    "testAssembly": {
      "type": "string",
      "description": "Specific test assembly name to run (optional). Example: \u0027Assembly-CSharp-Editor-testable\u0027"
    },
    "testNamespace": {
      "type": "string",
      "description": "Specific test namespace to run (optional). Example: \u0027MyTestNamespace\u0027"
    },
    "testClass": {
      "type": "string",
      "description": "Specific test class name to run (optional). Example: \u0027MyTestClass\u0027"
    },
    "testMethod": {
      "type": "string",
      "description": "Specific fully qualified test method to run (optional). Example: \u0027MyTestNamespace.FixtureName.TestName\u0027"
    },
    "includePassingTests": {
      "type": "boolean",
      "description": "Include details for all tests, both passing and failing (default: false). If you just need details for failing tests, set to false."
    },
    "includeMessages": {
      "type": "boolean",
      "description": "Include test result messages in the test results (default: true). If you just need pass/fail status, set to false."
    },
    "includeStacktrace": {
      "type": "boolean",
      "description": "Include stack traces in the test results (default: false)."
    },
    "includeLogs": {
      "type": "boolean",
      "description": "Include console logs in the test results (default: false)."
    },
    "logType": {
      "type": "string",
      "enum": [
        "Error",
        "Assert",
        "Warning",
        "Log",
        "Exception"
      ],
      "description": "Log type filter for console logs. Options: \u0027Log\u0027, \u0027Warning\u0027, \u0027Assert\u0027, \u0027Error\u0027, \u0027Exception\u0027. (default: \u0027Warning\u0027)"
    },
    "includeLogsStacktrace": {
      "type": "boolean",
      "description": "Include stack traces for console logs in the test results (default: false). This is huge amount of data, use only if really needed."
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestRunResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestSummaryData": {
      "type": "object",
      "properties": {
        "Status": {
          "type": "string",
          "enum": [
            "Unknown",
            "Passed",
            "Failed"
          ]
        },
        "TotalTests": {
          "type": "integer"
        },
        "PassedTests": {
          "type": "integer"
        },
        "FailedTests": {
          "type": "integer"
        },
        "SkippedTests": {
          "type": "integer"
        },
        "Duration": {
          "type": "string"
        }
      },
      "required": [
        "Status",
        "TotalTests",
        "PassedTests",
        "FailedTests",
        "SkippedTests",
        "Duration"
      ]
    },
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string"
        },
        "Status": {
          "type": "string",
          "enum": [
            "Passed",
            "Failed",
            "Skipped"
          ]
        },
        "Duration": {
          "type": "string"
        },
        "Message": {
          "type": "string"
        },
        "StackTrace": {
          "type": "string"
        }
      },
      "required": [
        "Status",
        "Duration"
      ]
    },
    "System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry\u003E": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry": {
      "type": "object",
      "properties": {
        "Condition": {
          "type": "string"
        },
        "StackTrace": {
          "type": "string"
        },
        "Type": {
          "type": "string",
          "enum": [
            "Error",
            "Assert",
            "Warning",
            "Log",
            "Exception"
          ]
        },
        "Timestamp": {
          "type": "string",
          "format": "date-time"
        },
        "LogLevel": {
          "type": "integer"
        }
      },
      "required": [
        "Type",
        "Timestamp"
      ]
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestRunResponse": {
      "type": "object",
      "properties": {
        "Summary": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestSummaryData",
          "description": "Summary of the test run including total, passed, failed, and skipped counts."
        },
        "Results": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData\u003E",
          "description": "List of individual test results with details about each test."
        },
        "Logs": {
          "$ref": "#/$defs/System.Collections.Generic.List\u003Ccom.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry\u003E",
          "description": "Log entries captured during test execution."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

