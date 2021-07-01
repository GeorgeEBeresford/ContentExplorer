using System.Collections.Generic;
using System.IO;
using System.Linq;
using NReco.VideoConverter;

namespace ContentExplorer.Services
{
    public class VideoThumbnailService : IThumbnailService
    {
        public void CreateThumbnail(string videoFullName)
        {
            string thumbnailFileLocation = $"{videoFullName}.jpg";
            CreateThumbnail(videoFullName, 240);

            FileInfo thumbnailInfo = new FileInfo(thumbnailFileLocation);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                CreateThumbnail(videoFullName, 120);
            }

            thumbnailInfo = new FileInfo(thumbnailFileLocation);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                CreateThumbnail(videoFullName, 60);
            }

            thumbnailInfo = new FileInfo(thumbnailFileLocation);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                CreateThumbnail(videoFullName, 1);
            }

            // If the video is less than 1 second long, it should really be a gif
            thumbnailInfo = new FileInfo(thumbnailFileLocation);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                FileInfo fileInfo = new FileInfo(videoFullName);
                fileInfo.MoveTo(fileInfo.FullName + ".shouldbegif");
            }
        }

        public void DeleteThumbnailIfExists(string videoFullName)
        {
            string thumbnailFileLocation = $"{videoFullName}.jpg";
            FileInfo thumbnail = new FileInfo(thumbnailFileLocation);

            if (thumbnail.Exists)
            {
                thumbnail.Delete();
            }
        }

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

        private void CreateThumbnail(string videoFullName, int frametime)
        {
            string thumbnailFileLocation = $"{videoFullName}.jpg";
            using (Stream jpgStream = new FileStream(thumbnailFileLocation, FileMode.Create))
            {
                FFMpegConverter ffMpeg = new FFMpegConverter();

                try
                {
                    ffMpeg.GetVideoThumbnail(videoFullName, jpgStream, frametime);
                }
                catch (FFMpegException exception)
                {
                    // File can not be properly read so no thumbnail for us
                    if (exception.ErrorCode == 69)
                    {
                        FileInfo fileInfo = new FileInfo(videoFullName);
                        fileInfo.MoveTo(fileInfo.FullName + ".broken");
                    }
                }
            }
        }
    }
}