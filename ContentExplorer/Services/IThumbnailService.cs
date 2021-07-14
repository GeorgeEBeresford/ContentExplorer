using System.IO;

namespace ContentExplorer.Services
{
    public interface IThumbnailService
    {
        FileInfo GetDirectoryThumbnail(DirectoryInfo directory);
    }
}
