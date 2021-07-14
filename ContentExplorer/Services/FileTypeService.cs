using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ContentExplorer.Services
{
    public class FileTypeService
    {
        public bool IsFileVideo(string fileName)
        {
            string fileExtension = fileName.Split('.').LastOrDefault();
            bool isVideo = VideoExtensions.Any(validExtension => validExtension == fileExtension);

            return isVideo;
        }

        public bool IsFileImage(string fileName)
        {
            string fileExtension = fileName.Split('.').LastOrDefault();

            bool isImage = ImageExtensions.Any(validExtension =>
                validExtension == fileExtension ||
                fileName.EndsWith($".thumbnail.{validExtension}")
            );

            return isImage;
        }

        private static readonly string[] VideoExtensions =
        {
            "mp4",
            "mpeg",
            "webm",
            "mkv",
            "avi",
            "mov",
            "m4v"
        };

        private static readonly string[] ImageExtensions =
        {
            "jpg",
            "jpeg",
            "gif",
            "png",
            "tga",
            "bmp"
        };
    }
}