# DirectoryService

Project layout:

- `backend/` - .NET backend for the directory service
- `client/` - reserved for the future Next.js frontend

Backend solution:

- `backend/DirectoryService.sln`
- API entry point: `backend/src/DirectoryService.Presentation/Program.cs`

After this structure, you can create the frontend from the repository root with:

```bash
npx create-next-app@latest client
```
