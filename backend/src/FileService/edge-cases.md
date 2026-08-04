# FileService Edge Cases

Use this file as a checklist before changing FileService upload, storage, media lifecycle, or video processing behavior.

## Public Boundary

- Do not expose `UploadedKey`, `RawKey`, `FinalKey`, bucket names, object paths, or S3 implementation details through public contracts.
- Do not store presigned/content URLs as long-lived data in consumers. `GetContentUrlResponse.Url` is short-lived.
- External services should use `MediaAsset.Id` through `FileService.Contracts`/`FileService.Communication`, not FileService domain objects.

## Upload Lifecycle

- Cancelling an upload should delete/abort the uploaded object only when the asset is still in a cancellable state.
- Completing upload should validate object metadata and not mark missing/invalid storage objects as successful uploads.
- Multipart abort should use the original upload storage key and upload id.
- Repeated complete/cancel calls should not silently corrupt state.
- Storage provider failures should remain distinguishable from user validation errors.

## Storage Keys

- `UploadedKey` belongs to upload/cancel/complete-upload flows.
- `RawKey` is the processing source object.
- `FinalKey` is the ready object or HLS manifest used for consumption/playback.
- Do not mix raw/upload/final keys in processing or public responses.

## Media Status

- Domain methods should own lifecycle transitions. Do not set status directly from handlers or infrastructure code.
- Invalid repeated transitions should return validation/conflict-style errors, not silently succeed.
- Deleted or failed assets should not be treated as ready downloadable content.

## FileService.Communication

- Preserve distinction between timeout, cancellation, service unavailable, invalid response envelope, and domain errors returned by FileService.
- Keep transport DTO compatibility in mind when changing `FileService.Contracts`.
- Public client methods should not leak internal FileService storage details.

## Video Processing

- Pipeline steps must not be ASP.NET handlers and should be testable without a web host.
- Any step failure, missing handler, or cancellation should stop later steps and mark the video asset as `FAILED`.
- Successful completion should mark the video asset as `READY` or another explicit FS-9-compatible prepared state.
- Do not leave assets in endless `PROCESSING`.
- Keep scheduler/background launch out of `ProcessingPipeline`; that belongs to a separate layer/task.
- Mock steps should keep deterministic working directories so real cleanup can replace them later.
- FS-10 should not require real FFmpeg. Real ffprobe/transcode/HLS belongs to FS-11.

## Persistence

- EF mapping changes should be paired with migrations.
- Repository queries should preserve owner/context filters where applicable.
- FileService schema is expected to use the `files` schema for FileService tables.

## Tests To Check

From `backend/`:

```powershell
dotnet test "tests\FileService.Domain.UnitTests\FileService.Domain.UnitTests.csproj" --filter FullyQualifiedName~ProcessingPipelineTests
dotnet test "tests\FileService.IntegrationTests\FileService.IntegrationTests.csproj"
```
