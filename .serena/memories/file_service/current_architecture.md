# File Service current architecture

- Public transport DTOs live in `backend/src/FileService/FileService.Contracts`.
- Typed HTTP client lives in `FileService.Communication`; it references Contracts and returns `Result<T, Errors>`.
- `BaseHttpClient` handles HTTP envelope deserialization. Current Shared packages can emit errors as `errorList`, while generic envelopes use `errors`; the internal `HttpEnvelope<T>` compatibility model accepts both.
- FileService Minimal API endpoints live in Core and now return Shared envelopes. Query endpoints use `EndpointResult<T>`; command/unit endpoints return `Envelope.Ok()` or `ErrorsResult`.
- `ErrorsResult` maps Validation→400, NotFound→404, Conflict→409, Failure→500. Do not use `Error.Failure` for expected validation problems.
- S3/MinIO details stay in Infrastructure.S3. `S3ErrorMapper` recognizes HTTP 404 as object-not-found; CompleteUpload maps a missing uploaded object to validation while preserving real network/internal errors.
- Integration tests use Testcontainers PostgreSQL and MinIO and require Docker Desktop. Last verified full FileService suite: 21/21 passing.
- Successful integration-test response bodies should be read through `ReadEnvelopeResultAsync<T>`.
