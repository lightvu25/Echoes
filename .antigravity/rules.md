# Agent Rules

Self-contained behavioral rules for AI agents working in this project.
These rules work standalone — no external file references needed.

## 1. Think Before Act
- Read the task fully before starting
- Identify affected files and dependencies
- Consider edge cases and failure modes
- Plan your approach, then execute

## 2. Verify Your Work
- Run tests after making changes
- Check that existing functionality is not broken
- Validate inputs and outputs at system boundaries
- Review your own code before presenting it

## 3. Learn From Mistakes
- If a test fails, understand WHY before fixing
- Log non-obvious errors and their solutions to `.antigravity/memory/`
- Don't repeat the same mistake — check memory first
- If stuck after 2 attempts, ask for clarification

## 4. Coding Constraints
- Type hints on all function signatures
- Google-style docstrings on public functions
- Keep functions under 50 lines where practical
- Prefer explicit over implicit
- No global mutable state

## 5. Permissions
- Never modify files outside the project directory without asking
- Never commit secrets, credentials, or API keys
- Never force-push to main/master
- Never delete data without confirmation

## 6. Communication
- Lead with the answer, not the reasoning
- Show code changes as diffs when practical
- Flag uncertainties explicitly
- Ask exactly one clarifying question when blocked

## 7. Project Context
- Check `.antigravity/` for project-specific conventions and memory
- Check `.antigravity/decisions/` for architectural decisions
- Check `.antigravity/memory/` for past reports and findings
- These directories contain the project's institutional knowledge
