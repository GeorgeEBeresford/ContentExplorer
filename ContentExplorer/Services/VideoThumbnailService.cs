using System.Collections.Generic;
using System.IO;
using System.Linq;
using NReco.VideoConverter;

namespace ContentExplorer.Services
{
    public class VideoThumbnailService : IThumbnailService
    {
        public void CreateThumbnail(FileInfo fileInfo)
        {
            FileInfo fileThumbnail = GetFileThumbnail(fileInfo);
            if (fileThumbnail.Exists)
            {
                return;
            }

            CreateThumbnail(fileInfo, 240);

            FileInfo thumbnailInfo = GetFileThumbnail(fileInfo);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                CreateThumbnail(fileInfo, 120);
            }

            thumbnailInfo = GetFileThumbnail(fileInfo);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                CreateThumbnail(fileInfo, 60);
            }

            thumbnailInfo = GetFileThumbnail(fileInfo);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                CreateThumbnail(fileInfo, 1);
            }

            // If the video is less than 1 second long, it should really be a gif
            thumbnailInfo = GetFileThumbnail(fileInfo);
            if (thumbnailInfo.Exists == false || thumbnailInfo.Length == 0)
            {
                fileInfo.MoveTo(fileInfo.FullName + ".shouldbegif");
            }
        }

        public void DeleteThumbnailIfExists(FileInfo fileInfo)
        {
            FileInfo thumbnail = GetFileThumbnail(fileInfo);
            if (thumbnail.Exists)
            {
                thumbnail.Delete();
            }
        }

        public FileInfo GetDirectoryThumbnail(DirectoryInfo directory)
        {
            FileTypeService fileTypeService = new FileTypeService();

            FileInfo previewImage = directory
                .EnumerateFiles()
                .FirstOrDefault(file => fileTypeService.IsFileVideo(file.Name));

            if (previewImage != null)
            {
                return previewImage;
            }

            IEnumerable<DirectoryInfo> subDirectories =
                directory.EnumerateDirectories("*", SearchOption.AllDirectories);

            FileInfo firstPreview = subDirectories
                .Select(GetDirectoryThumbnail)
                .FirstOrDefault(thumbnail => thumbnail != null);

            return firstPreview;
        }

        public FileInfo GetFileThumbnail(FileInfo fileInfo)
        {
            string thumbnailPath = $"{fileInfo.FullName}.jpg";
            FileInfo directoryThumbnail = new FileInfo(thumbnailPath);

            return directoryThumbnail;
        }

        public bool IsThumbnail(FileInfo fileInfo)
        {
            bool isThumbnail = fileInfo.Name.EndsWith(".jpg");
            return isThumbnail;
        }

        private void CreateThumbnail(FileInfo videoFileInfo, int frametime)
        {
            FileInfo thumbnailLocation = GetFileThumbnail(videoFileInfo);
            using (Stream jpgStream = new FileStream(thumbnailLocation.FullName, FileMode.Create))
            {
                FFMpegConverter ffMpeg = new FFMpegConverter();

                try
                {
                    ffMpeg.GetVideoThumbnail(videoFileInfo.FullName, jpgStream, frametime);
                }
                catch (FFMpegException exception)
                {
                    // File can not be properly read so no thumbnail for us
                    if (exception.ErrorCode == 69)
                    {
                        videoFileInfo.MoveTo(videoFileInfo.FullName + ".broken");
                    }
                }
            }
        }
    }
}