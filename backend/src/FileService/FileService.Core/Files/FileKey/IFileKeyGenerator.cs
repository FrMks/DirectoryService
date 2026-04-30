using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileService.Core.Files.FileKey;

public interface IFileKeyGenerator
{
    string GenerateRawFileKey(FileKeyContext context);
}