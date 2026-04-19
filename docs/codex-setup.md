# Codex Setup For This Repository

This repository keeps project-specific Codex setup files in version control so the same setup can be reproduced on another computer.

## What Lives In Git

- `AGENTS.md` files
  Project navigation and architecture rules for Codex.
- `tools/codex/skills/karpathy-guidelines/`
  Project-approved copy of the Karpathy-style Codex skill.
- `tools/codex/config-snippets/`
  MCP config snippets for this project.
- `tools/codex/setup-codex.ps1`
  PowerShell installer for this repository's preferred Codex setup.

## What Does Not Live In Git

Codex itself loads active global configuration from the user profile, not from the repository.

On Windows, the active locations are:

- `C:\Users\<you>\.codex\config.toml`
- `C:\Users\<you>\.codex\skills\`
- `C:\Users\<you>\.serena\`

That means this repository can version the desired setup, but each computer still needs a local install step.

## Install On A New Computer

From the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\codex\setup-codex.ps1
```

The script:

1. Copies the repository's `karpathy-guidelines` skill into `~/.codex/skills/`
2. Ensures `Context7` exists in `~/.codex/config.toml`
3. Installs `uv` if needed
4. Installs `Serena`
5. Initializes Serena if needed
6. Ensures `Serena` exists in `~/.codex/config.toml`

After running the script, restart Codex.

## Restarting Codex

If you use the Codex desktop app:

1. Close the app window completely
2. Quit it from the tray/taskbar if it is still running
3. Open the app again

If you use Codex inside an IDE:

1. Close the Codex panel/session
2. Reload the IDE window if the extension keeps the old session alive
3. Start a new Codex session

If you are unsure, the safest option is:

1. Close the IDE
2. Close the Codex desktop app
3. Re-open the IDE or the app

## When To Use Which Tool

- `AGENTS.md`
  Always helpful. Codex uses these as project-local instructions.
- `karpathy-guidelines`
  Use when you want especially careful reasoning, minimal diffs, and explicit validation.
- `Context7`
  Use when the task depends on current library or framework docs.
- `Serena`
  Use when you need better codebase navigation, symbol lookup, references, and large-project understanding.

## Important Distinction

`karpathy-guidelines` is a behavior skill.
It changes how Codex approaches the task.

`Serena` is an MCP tool server.
It gives Codex extra capabilities for code intelligence.

They solve different problems and work well together.
