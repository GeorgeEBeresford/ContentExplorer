using ContentExplorer.Models;
using System.Collections.Generic;
using System.IO;

namespace ContentExplorer.Services
{
    public interface IFileSystemFilteringService
    {
        bool FileMatchesFilter(IEnumerable<TagLink> tagLinksforFile, string filterString);
        bool FileMatchesFilter(FileInfo fileInfo, string filterString);
    }
}
