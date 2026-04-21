# DirectoryService Service Codex Guide

## Scope

This file applies to `backend/src/DirectoryService/`.

## Purpose

This is the main bounded context of the backend.
Use this file to keep Codex focused on the correct service layer and avoid unnecessary scanning.

## Service Layers

- `DirectoryService.Domain` - entities, invariants, value objects, core business concepts
- `DirectoryService.Contracts` - DTOs and request/response contracts
- `DirectoryService.Application` - commands, queries, handlers, validators, abstractions
- `DirectoryService.Infrastructure.Postgres` - persistence, EF Core config, migrations, repository implementations, background services
- `DirectoryService.Presentation` - controllers, host startup, DI wiring, configuration

## Layer Rules

- Keep business rules in `Domain` or `Application`, not in controllers.
- Keep transport models in `Contracts`.
- Keep Postgres and EF-specific code in `Infrastructure.Postgres`.
- Keep controllers thin and delegate to application handlers/services.
- Avoid new dependencies that break the direction defined in `backend/AGENTS.md`.

## Change Strategy

- API shape change: inspect `Contracts`, `Application`, `Presentation`, and integration tests together.
- Persistence change: inspect repositories, EF configurations, and migrations together.
- Domain model change: verify validators, mapping, and dependent handlers still align.
