using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileService.Core.Files.FileKey;

public sealed record FileKeyContext
(
    string OriginalFileName,
    string ContentType,
    Guid? OwnerId = null
);