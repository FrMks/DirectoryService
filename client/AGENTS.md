## Client Codex Guide

## Scope

This file applies to `client/`.

## Purpose

Help Codex work on the frontend without spending tokens on backend discovery and without assuming outdated Next.js behavior.

## Framework Note

This Next.js version may differ from Codex training data.
Before using unfamiliar Next.js APIs, check local docs in `node_modules/next/dist/docs/`.

## Structure

- `src/app/` - App Router pages, layout, and global styles
- `public/` - static assets
- `package.json` - scripts and dependencies

## Working Rules

- Preserve the existing App Router structure unless the task requires a broader refactor.
- Prefer small, intentional UI changes over wide redesigns unless requested.
- Keep global styles in `src/app/globals.css` unless a local style split is clearly better.
- Treat backend as an external API boundary.
- Do not introduce direct code sharing from `backend/` into `client/`.
