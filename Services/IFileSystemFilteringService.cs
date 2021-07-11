using ContentExplorer.Models;
using System.Collections.Generic;
using System.IO;

namespace ContentExplorer.Services
{
    public interface IFileSystemFilteringService
    {
        bool TagLinkMatchesFilter(IEnumerable<TagLink> tagLinksforFile, string[] filters);
        bool TagsMatchFilter(IEnumerable<Tag> tags, IEnumerable<string> filters);
        bool FileMatchesFilter(FileInfo fileInfo, string[] filters);
        bool DirectoryMatchesFilter(DirectoryInfo directoryInfo, string[] filters, string mediaType);
    }
}
