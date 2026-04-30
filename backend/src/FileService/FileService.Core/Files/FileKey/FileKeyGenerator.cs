using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileService.Core.Files.FileKey;

public sealed class FileKeyGenerator : IFileKeyGenerator
{
    public string GenerateRawFileKey(FileKeyContext context)
    {
        var extension = Path.GetExtension(context.OriginalFileName);
        var date = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var id = Guid.NewGuid().ToString("D");

        return $"raw/{date}/{id}{extension}";
    }
}