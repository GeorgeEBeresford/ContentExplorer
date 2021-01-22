using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NReco.VideoConverter;

namespace ContentExplorer.Services
{
    public class VideoConversionService
    {
        public void ConvertUnplayableVideos(DirectoryInfo directory)
        {
            IEnumerable<DirectoryInfo> subDirectories = directory.GetDirectories();

            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                ConvertUnplayableVideos(subDirectory);
            }

            IEnumerable<FileInfo> unplayableVideos = directory
                .EnumerateFiles()
                .Where(potentialVideo =>
                    UnplayableVideoTypes.Any(unplayableVideoType =>

                        potentialVideo.Extension.Equals(unplayableVideoType, StringComparison.OrdinalIgnoreCase)
                    )
                );


            FFMpegConverter ffMpegConverter = new FFMpegConverter();

            foreach (FileInfo unplayableVideo in unplayableVideos)
            {
                int indexOfExtension = unplayableVideo.Name.LastIndexOf(".");
                string videoType = unplayableVideo.Name.Substring(indexOfExtension + 1);
                ConvertSettings convertSettings = new ConvertSettings();

                ffMpegConverter.ConvertMedia(unplayableVideo.FullName, videoType, $"{unplayableVideo.FullName}.mp4", "mp4", convertSettings);
                unplayableVideo.MoveTo(Path.Combine(unplayableVideo.Directory.FullName, unplayableVideo.Name + ".old"));
            }

        }

        private static readonly string[] UnplayableVideoTypes = new[]
        {
            ".avi"
        };
    }
}