using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using NReco.VideoConverter;

namespace ContentExplorer.Services
{
    public class ImageThumbnailService
    {
        public FileInfo GetImageThumbnail(DirectoryInfo directory)
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
                    subVideoThumbnail = GetImageThumbnail(subDirectory);
                }

                return subVideoThumbnail;
            }

            return null;
        }
    }
}