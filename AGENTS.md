# Codex Guide

## Scope

This file applies to the whole repository.
More specific `AGENTS.md` files in subfolders override it for their area.

## Goal

Give Codex a short map of the repo so it can open fewer files, stay in the correct layer, and spend fewer tokens on discovery.

## Repository Map

- `backend/` - .NET backend, Docker config, and integration tests
- `client/` - Next.js frontend
- `README.md` - high-level project overview

## How Codex Should Work Here

- Read the closest `AGENTS.md` before editing a folder.
- Stay inside the current layer unless the task clearly crosses boundaries.
- Prefer targeted reads over broad repo scans.
- Avoid unrelated refactors while solving the requested task.
- When moving or renaming backend projects, update solution, project references, test references, and Docker paths in the same change.

## Architecture Boundaries

- Backend owns business rules and API behavior.
- Client consumes backend over HTTP and should not reference backend code directly.
- Domain rules belong in backend domain/application layers, not in frontend code.

## Navigation

- Backend solution: `backend/DirectoryService.sln`
- Backend entry point: `backend/src/DirectoryService/DirectoryService.Presentation/Program.cs`
- Frontend app root: `client/src/app`
