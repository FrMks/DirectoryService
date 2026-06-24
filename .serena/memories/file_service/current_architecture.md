# File Service current architecture

- Public transport DTOs live in `backend/src/FileService/FileService.Contracts`.
- Typed HTTP client lives in `FileService.Communication`; it references Contracts and returns `Result<T, Errors>`.
- Public read client methods: `GetMediaAssetByIdAsync`, `GetFilesByOwnerAsync`, and `GetContentUrlAsync`. Shared GET handling distinguishes cancellation, timeout, unavailable service, envelope/domain errors, and unexpected client failures.
- `FileServiceExtensions` registers strongly typed options and `AddHttpClient<IFileCommunicationService, FileHttpClient>`; BaseAddress must be configured from options. Timeout is currently configured during registration and should remain externally configurable.
- `BaseHttpClient` handles HTTP envelope deserialization. Current Shared packages can emit errors as `errorList`, while generic envelopes use `errors`; internal `HttpEnvelope<T>` accepts both.
- Minimal API endpoints live in Core and return Shared envelopes. Query endpoints use `EndpointResult<T>`; command/unit endpoints return `Envelope.Ok()` or `ErrorsResult`.
- Error mapping: Validation→400, NotFound→404, Conflict→409, Failure→500. Expected bad requests must not use `Error.Failure`.
- S3 details stay in Infrastructure.S3. Missing uploaded objects are mapped carefully so validation remains distinct from real network/internal failures.
- Full FileService integration suite last verified: 21/21 passing with Docker/Testcontainers.
- NuGet packages configured and locally packed at version 0.0.1: `FileService.Contracts` and `FileService.Communication`. Communication package declares dependency on Contracts 0.0.1. Publish Contracts first, then Communication, to GitHub source `https://nuget.pkg.github.com/FrMks/index.json`.
- Current pack warnings are non-blocking StyleCop warnings: missing trailing newlines (SA1518) and two types in `HttpEnvelope.cs` (SA1402).
