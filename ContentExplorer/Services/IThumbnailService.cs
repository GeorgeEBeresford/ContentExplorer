using System.IO;

namespace ContentExplorer.Services
{
    public interface IThumbnailService
    {
        void CreateThumbnail(FileInfo fileInfo);
        void DeleteThumbnailIfExists(FileInfo fileInfo);
        FileInfo GetDirectoryThumbnail(DirectoryInfo directory);
        FileInfo GetFileThumbnail(FileInfo fileInfo);
        bool IsThumbnail(FileInfo fileInfo);
    }
}