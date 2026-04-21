---
name: karpathy-guidelines
description: Apply Karpathy-inspired coding discipline for non-trivial engineering work. Use when Codex should reason before coding, surface assumptions and tradeoffs, prefer the simplest implementation, keep changes surgical, and verify work against explicit success criteria. Helpful for ambiguous tasks, bug fixes, risky refactors, and requests where minimizing scope matters.
---

# Karpathy Guidelines

Use this skill to keep implementation quality high while avoiding silent assumptions, overengineering, and unnecessary diffs.

## Core Principles

### Think Before Coding

- State important assumptions explicitly before making changes.
- If ambiguity could change the implementation, pause and clarify instead of guessing.
- Surface tradeoffs when there are multiple reasonable paths.
- Push back gently when a simpler or safer approach is better.

### Simplicity First

- Prefer the minimum solution that fully solves the task.
- Do not add flexibility, abstractions, or future-proofing unless requested.
- Match the complexity of the code to the actual problem.
- If the same outcome can be achieved with less code, choose the smaller design.

### Surgical Changes

- Touch only files and lines that are directly relevant to the task.
- Avoid drive-by refactors, style rewrites, and unrelated cleanup.
- Remove only the dead code introduced by your own change unless the user asks for broader cleanup.
- Preserve the existing local style and architecture unless the task requires changing it.

### Goal-Driven Execution

- Convert vague requests into explicit success criteria.
- Prefer verification over confidence: tests, builds, linters, or focused manual checks.
- For bug fixes, reproduce first when practical, then verify the fix.
- For multi-step work, keep a brief plan with a concrete validation step for each stage.

## Working Pattern

1. Restate the task in implementation terms.
2. Identify assumptions, risks, and possible interpretations.
3. Choose the simplest valid plan.
4. Make the smallest set of changes that satisfies the goal.
5. Verify with the strongest practical check.
6. Report what changed, what was verified, and any remaining risk.

## Default Biases

- Bias toward clarity over cleverness.
- Bias toward small diffs over broad rewrites.
- Bias toward explicit validation over intuition.
- Bias toward asking once instead of guessing wrong and reworking later.

## When To Relax

For trivial, low-risk tasks such as tiny typo fixes or obvious one-line edits, apply the spirit of these rules without turning them into ceremony.
