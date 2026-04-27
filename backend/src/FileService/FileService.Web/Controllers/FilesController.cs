using FileService.Core.Files;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Core.Abstractions;
using Shared.Framework.EndpointResults;

namespace FileService.Web.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<EndpointResult<string>> Upload(
        [FromServices] ICommandHandler<string, UploadFileCommand> handler,
        [FromForm] IFormFile formFile,
        CancellationToken cancellationToken)
    {
        var key = $"raw/{Guid.NewGuid()}";
        await using var stream = formFile.OpenReadStream();

        UploadFileCommand command = new(
            stream,
            "pictures",
            key,
            formFile.ContentType);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<string>();

        return result;
    }
}
