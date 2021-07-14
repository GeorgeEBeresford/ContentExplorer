using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContentExplorer.Services
{
    public class ImageThumbnailService : IThumbnailService
    {
        public FileInfo GetDirectoryThumbnail(DirectoryInfo directory)
        {
            FileTypeService fileTypeService = new FileTypeService();

            FileInfo previewVideo = directory
                .EnumerateFiles()
                .FirstOrDefault(file => fileTypeService.IsFileImage(file.Name));

            if (previewVideo != null)
            {
                return previewVideo;
            }

            ICollection<DirectoryInfo> subDirectories = directory.GetDirectories();

            if (subDirectories.Any())
            {
                FileInfo subVideoThumbnail = null;

                for (int subDirectoryIndex = 0; subDirectoryIndex < subDirectories.Count; subDirectoryIndex++)
                {
                    DirectoryInfo subDirectory = subDirectories.ElementAt(subDirectoryIndex);
                    subVideoThumbnail = GetDirectoryThumbnail(subDirectory);
                }

                return subVideoThumbnail;
            }

            return null;
        }
    }
}