# Location preview attachment progress

- Scenario chosen: Location preview image backed by FileService media asset.
- DirectoryService stores a nullable `PreviewAssetId` on `Location`; this is the public FileService `MediaAssetId`/`FileResponse.Id` (`Guid`), not a storage key, presigned URL, or FileService internal state.
- Value object: `backend/src/DirectoryService/DirectoryService.Domain/Locations/ValueObjects/MediaAssetId.cs` wraps the external `Guid`. DirectoryService should not generate FileService asset ids itself.
- EF mapping: `backend/src/DirectoryService/DirectoryService.Infrastructure.Postgres/Configurations/LocationConfiguration.cs` maps `PreviewAssetId` to nullable `uuid` column `preview_asset_id` using nullable `ValueConverter<MediaAssetId?, Guid?>`.
- Migration created/applied locally: `20260624045953_AddPreviewAssetidToLocation`, adding nullable `preview_asset_id` to `locations`; snapshot updated.
- Next steps: explicit attach/replace/delete operations, validation through `FileService.Communication`, DTO read enrichment/degraded read behavior when FileService is unavailable.