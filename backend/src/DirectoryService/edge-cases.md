# DirectoryService Edge Cases

Use this file as a checklist before changing DirectoryService domain, application handlers, API contracts, persistence, or FileService attachment behavior.

## Layer Boundaries

- Keep business rules in `DirectoryService.Domain` or `DirectoryService.Application`, not controllers.
- Keep transport DTOs in `DirectoryService.Contracts`.
- Keep EF/Postgres-specific code in `DirectoryService.Infrastructure.Postgres`.
- DirectoryService domain must not reference FileService domain or infrastructure projects.

## API And Contracts

- API shape changes should be checked across Contracts, Application handlers/validators, Presentation controllers, and integration tests.
- Expected validation failures should not be represented as unexpected internal failures.
- Controllers should stay thin and delegate behavior to application handlers/services.

## Persistence

- Entity mapping changes should be paired with migrations.
- Repository changes should preserve filtering, hierarchy, and ownership semantics.
- Domain model changes should be checked against validators, mapping, dependent handlers, and tests.

## Department And Hierarchy

- Parent changes should not create cycles.
- Level/path/depth calculations should remain consistent after moving nodes.
- Deleting or cleaning departments should not orphan children unexpectedly.
- Updates should preserve invariants for parent id, hierarchy level, and related locations/positions.

## Locations

- Location updates should preserve existing preview metadata unless an explicit attach/replace/delete preview operation is requested.
- Location preview asset ids should be FileService public `MediaAssetId` values; DirectoryService should not generate FileService asset ids itself.
- Location preview metadata is local display/degraded-read data, not storage ownership.

## FileService Attachments

- DirectoryService may store the public FileService `MediaAssetId` and local display metadata only.
- Do not persist FileService storage keys, S3 paths, presigned/content URLs, or internal FileService lifecycle state.
- Attach/replace operations should explicitly validate the asset through `FileService.Communication` before saving local binding.
- For location previews, require a READY, not DELETED, PREVIEW/image asset whose owner context/entity id matches the target location.
- FileService unavailable/timeout behavior should be handled explicitly; avoid silently attaching stale or unverified assets.

## Cross-Service Communication

- Use `FileService.Contracts` and `FileService.Communication` for FileService integration.
- Preserve distinction between cancellation, timeout, service unavailable, FileService domain errors, and malformed responses.
- Do not couple DirectoryService domain logic to FileService internal lifecycle implementation details.

## Tests To Check

From repository root:

```powershell
dotnet test .\backend\DirectoryService.sln
```

For targeted DirectoryService integration verification from `backend/`:

```powershell
dotnet test "tests\DirectoryService.IntegrationTests\DirectoryService.IntegrationTests.csproj"
```
