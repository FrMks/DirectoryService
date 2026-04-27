# FileService Codex Guide

## Scope

This file applies to `backend/src/FileService/`.

## Purpose

FileService handles file upload/storage workflows.
Use this guide to keep transport, orchestration, and infrastructure separated.

## Service Layers

- `FileService.Domain` - file-service domain concepts
- `FileService.Contracts` - DTOs and request/response contracts when a transport contract is needed
- `FileService.Core` - commands, handlers, interfaces for infrastructure dependencies, caching orchestration
- `FileService.Infrastructure.Postgres` - Postgres-specific persistence
- `FileService.Infrastructure.S3` - S3/MinIO implementation of Core storage interfaces
- `FileService.Web` - controllers, host startup, DI wiring, configuration

## Layer Rules

- Keep controllers thin; delegate work to Core handlers.
- Keep infrastructure details such as AWS SDK, MinIO endpoints, and S3 clients in `Infrastructure.S3`.
- Put interfaces needed by handlers in `FileService.Core`, then implement them in infrastructure projects.
- `FileService.Core` should not reference `FileService.Infrastructure.*`.
- Use `Shared.Core.Abstractions` command/query handler style when adding handlers.

## Change Strategy

- Upload API change: inspect `FileService.Web`, `FileService.Core`, and `FileService.Infrastructure.S3` together.
- S3 behavior change: inspect `S3Options`, S3 DI, and the Core storage interface together.
- New endpoint: add a controller action in `FileService.Web`, a command/query plus handler in `FileService.Core`, and infrastructure interfaces only when needed.
