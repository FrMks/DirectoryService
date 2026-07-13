# DirectoryService

Project layout:

- `backend/` - .NET backend for the directory service
- `client/` - reserved for the future Next.js frontend

Backend solution:

- `backend/DirectoryService.sln`
- API entry point: `backend/src/DirectoryService.Presentation/Program.cs`

Graphify code graph:

- Generated output path: `backend/graphify-out/`
- For broad dependency/codebase analysis, agents should first inspect `backend/graphify-out/graph.json` and `backend/graphify-out/.graphify_analysis.json` before doing wide source scans.
- Regenerate after large structural changes from `backend/` with `graphify . --code-only`.
- `backend/graphify-out/` is generated output and is ignored by git.

After this structure, you can create the frontend from the repository root with:

```bash
npx create-next-app@latest client
```
