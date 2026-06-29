# Location preview attachment progress

- Scenario chosen: Location preview image backed by FileService media asset.
- DirectoryService stores `Location.PreviewMetadata`, not bytes/storage keys/presigned URLs/FileService internal lifecycle state.
- `MediaAssetId` wraps the public FileService `MediaAssetId`/`FileResponse.Id` (`Guid`). DirectoryService should not generate FileService asset ids itself.
- `LocationPreviewMetadata` stores local display/degraded metadata: asset id, file name, content type, size, attached time, last verified time.
- EF mapping in `LocationConfiguration` stores preview metadata in `locations` columns: `preview_asset_id`, `preview_file_name`, `preview_content_type`, `preview_size`, `preview_attached_at`, `preview_last_verified_at`.
- Persistence migrations: `20260624045953_AddPreviewAssetidToLocation` and `20260624100128_AddLocationPreviewMetadata`.
- Explicit attach endpoint: `PUT /api/locations/{locationId:guid}/preview-asset` in `Locations.cs`, body `AttachLocationPreviewRequest { MediaAssetId }`.
- Attach handler: `AttachLocationPreviewHandler` loads Location, calls `IFileCommunicationService.GetMediaAssetByIdAsync`, requires status READY, not DELETED, asset type PREVIEW, content type image/*, context `location`, and ContextId equal to Location id. Then creates metadata, calls `Location.AttachPreview`, and saves with `ITransactionManager.SaveChangesAsync`.
- Build verified after attach work: `dotnet build backend/src/DirectoryService/DirectoryService.Presentation/DirectoryService.Presentation.csproj` passes with warnings only.
- Next steps: explicit replace/delete semantics if needed, then read DTO enrichment/degraded read behavior when FileService is unavailable.