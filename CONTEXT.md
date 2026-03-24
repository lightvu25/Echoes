# 🧠 Project Context

> **First Principle**: AI Agent capability = context quality. Architecture = files.

## Cognitive Loop

```
Think → Act → Reflect → Evolve
 ↑                        |
 └────────────────────────┘
```

| Phase | Rule | Output |
|:------|:-----|:-------|
| **Think** | Plan before coding. Reason through tradeoffs. | `artifacts/plan_*.md` |
| **Act** | Clean, typed, documented code. | Source files |
| **Reflect** | Test. Verify. Save evidence. | `artifacts/logs/` |
| **Evolve** | Document mistakes. Extract prevention rules. | `artifacts/error_journal.md` |

## Project Structure

```
your-project/
├── .antigravity/rules.md   # AI behavioral constraints
├── .cursorrules             # IDE cognitive bootstrap
├── CONTEXT.md               # This file
├── mission.md               # (optional) High-level objective
├── .context/                # (optional) Injected knowledge
├── artifacts/               # All AI outputs
│   ├── plan_*.md            #   Plans (BEFORE coding)
│   ├── logs/                #   Test & execution logs
│   ├── error_journal.md     #   Self-evolution tracking
│   └── screenshots/         #   UI evidence
└── src/                     # Source code
```

## Coding Standards

- **Type hints** on all functions
- **Google-style docstrings** on all functions/classes
- **Pydantic** for data models
- **Tool encapsulation** for external APIs
- **No silent exceptions**

## Self-Evolution

The architecture learns from mistakes:
1. Bug found → document in `artifacts/error_journal.md`
2. Lesson generalizable → extract into `.antigravity/rules.md`
3. Before acting → scan error journal for relevant past failures
4. **Never repeat a documented mistake**
