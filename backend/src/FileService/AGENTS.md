# FileService Codex Guide

## Scope

This file applies to `backend/src/FileService/`.

## Purpose

FileService handles file upload/storage workflows.
It uses Vertical Slice Architecture with Minimal API endpoint slices in `FileService.Core`.

## Service Layers

- `FileService.Domain` - file-service domain concepts
- `FileService.Contracts` - DTOs and request/response contracts when a transport contract is needed
- `FileService.Core` - Minimal API endpoint slices, orchestration, interfaces for infrastructure dependencies, caching
- `FileService.Infrastructure.Postgres` - Postgres-specific persistence
- `FileService.Infrastructure.S3` - S3/MinIO implementation of Core storage interfaces
- `FileService.Web` - host startup, middleware pipeline, DI wiring, configuration

## Layer Rules

- Prefer Minimal API endpoint groups/slices in `FileService.Core` over MVC controllers.
- Keep `FileService.Web` focused on startup and middleware; map Core endpoints from `Program.cs`.
- Keep infrastructure details such as AWS SDK, MinIO endpoints, and S3 clients in `Infrastructure.S3`.
- Put interfaces needed by endpoint slices in `FileService.Core`, then implement them in infrastructure projects.
- `FileService.Core` should not reference `FileService.Infrastructure.*`.

## Change Strategy

- Upload API change: inspect `FileService.Core`, `FileService.Web`, and `FileService.Infrastructure.S3` together.
- S3 behavior change: inspect `S3Options`, S3 DI, and the Core storage interface together.
- New endpoint: add or extend a Minimal API slice in `FileService.Core`, map it from `FileService.Web`, and add infrastructure interfaces only when needed.

## Public Asset Boundary

- `MediaAsset.Id` is exposed to other services as the public `MediaAssetId`/`FileResponse.Id` (`Guid`).
- Cross-service callers should use `FileService.Contracts` and `FileService.Communication`; `FileResponse` exposes file name, content type, size, status, asset type, owner context/entity id, optional content URL, and timestamps.
- `GetContentUrlResponse.Url` is short-lived and must not be stored by consumers as persistent data.
- `RawKey`, `FinalKey`, `UploadedObject.Key`, storage references, bucket/object paths, and S3 details are FileService internals.
