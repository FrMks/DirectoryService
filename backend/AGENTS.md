# Backend Codex Guide

## Scope

This file applies to `backend/`.
More specific `AGENTS.md` files under `src/DirectoryService/` override it there.

## Purpose

Help Codex work on the backend without opening unrelated frontend files or guessing the layer layout.

## Structure

- `DirectoryService.sln` - backend solution entry point
- `src/` - runtime projects and Dockerfile
- `tests/` - integration tests
- `docker-compose.yml` - local infrastructure

## Project Layout

- `src/DirectoryService/DirectoryService.Domain` - entities, value objects, invariants
- `src/DirectoryService/DirectoryService.Contracts` - DTOs and request/response models
- `src/DirectoryService/DirectoryService.Application` - handlers, validation, abstractions, orchestration
- `src/DirectoryService/DirectoryService.Infrastructure.Postgres` - EF Core, repositories, migrations, Postgres-specific code
- `src/DirectoryService/DirectoryService.Presentation` - API host, controllers, DI, app settings
- `tests/DirectoryService.IntegrationTests` - integration tests

## Dependency Direction

- `Presentation` may depend on `Application`, `Contracts`, `Domain`, `Infrastructure.Postgres`
- `Infrastructure.Postgres` may depend on `Application`, `Contracts`, `Domain`
- `Application` may depend on `Contracts`, `Domain`
- `Contracts` may depend on `Domain` only where already established
- `Domain` should remain the most independent layer

## Backend Rules

- Keep migrations in `Infrastructure.Postgres`.
- Keep controllers thin and move behavior into application handlers/services.
- Put repository interfaces in `Application` and implementations in `Infrastructure.Postgres`.
- If an API contract changes, check `Contracts`, `Presentation`, and integration tests together.
- If persistence changes, check repository code, EF configuration, and migrations together.

## Common Commands

- Build: `dotnet build .\DirectoryService.sln`
- Test: `dotnet test .\DirectoryService.sln`
