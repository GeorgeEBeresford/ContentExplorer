using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ContentExplorer.Models.ViewModels;
using ContentExplorer.Services;

namespace ContentExplorer.Controllers
{
    public class VideoController : Controller
    {
        [HttpGet]
        public ViewResult Index(string path = "", int? page = 1, string filter = null)
        {
            if (page == null || page < 1)
            {
                page = 1;
            }

            if (path == null)
            {
                path = "";
            }
            else
            {
                // Remove any dangerous characters to prevent escaping the current context
                path = path.Trim("/\\.".ToCharArray());
            }

            if (filter == null)
            {
                filter = "";
            }

            DirectoryInfo directoryInfo = GetCurrentDirectory(path);
            ICollection<FileInfo> validFiles = GetMatchingFiles(directoryInfo, filter).ToList();

            if (page > validFiles.Count)
            {
                page = validFiles.Count;
            }

            int videosPerPage = 50;
            ViewBag.VideosPerPage = videosPerPage;

            DirectoryViewModel videosViewModel = new DirectoryViewModel
            {
                FileInfos = validFiles
                    .Skip(videosPerPage * (page.Value - 1)).Take(videosPerPage)
                    .ToList(),
                FileCount = validFiles.Count(),
                DirectoryInfos = directoryInfo.GetDirectories()
            };

            ViewBag.Directory = directoryInfo;
            ViewBag.Page = page;
            ViewBag.Filter = filter;

            return View(videosViewModel);
        }

        [HttpGet]
        public ViewResult View(string path, int page, string filter = "")
        {
            if (page < 1)
            {
                page = 1;
            }

            if (filter == null)
            {
                filter = "";
            }

            DirectoryInfo currentDirectory = GetCurrentDirectory(path);
            ICollection<FileInfo> validFiles = GetMatchingFiles(currentDirectory, filter)
                .ToList();

            if (page > validFiles.Count)
            {
                page = validFiles.Count;
            }

            int fileCount = validFiles.Count();
            FileInfo firstFile = validFiles.ElementAt(page - 1);

            ViewBag.Video = firstFile;
            ViewBag.VideoCount = fileCount;
            ViewBag.Path = path;
            ViewBag.Id = page;
            ViewBag.Filter = filter;


            int maxPreviews = validFiles.Count > 15 ? 15 : validFiles.Count;
            // Zero-based index
            int pageIndex = page - 1;

            int startingPreview = pageIndex - 7 < 1 ? 1 : page - 7;
            ViewBag.StartingPreview = startingPreview;

            IEnumerable<FileInfo> previews = validFiles.Skip(startingPreview - 1).Take(maxPreviews);
            ViewBag.Previews = previews;

            return View();
        }

        [HttpGet]
        public ActionResult RebuildThumbnails(string path = "")
        {
            DirectoryInfo baseDirectory = GetCurrentDirectory(path);

            RebuildThumbnails(baseDirectory);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ReformatNames(string path = "")
        {
            DirectoryInfo baseDirectory = GetCurrentDirectory(path);

            ReformatNames(baseDirectory);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ConvertUnplayableVideos(string path = "")
        {
            DirectoryInfo baseDirectory = GetCurrentDirectory(path);
            VideoConversionService videoConversionService = new VideoConversionService();

            videoConversionService.ConvertUnplayableVideos(baseDirectory);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private ActionResult RebuildThumbnails(DirectoryInfo directory)
        {
            IEnumerable<DirectoryInfo> subDirectories = directory.GetDirectories();

            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                RebuildThumbnails(subDirectory);
            }

            VideoThumbnailService videoThumbnailService = new VideoThumbnailService();
            IEnumerable<FileInfo> videos = GetMatchingFiles(directory, "");
            foreach (FileInfo video in videos)
            {
                videoThumbnailService.DeleteThumbnailIfExists(video.FullName);
                videoThumbnailService.CreateThumbnail(video.FullName);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private ActionResult ReformatNames(DirectoryInfo directory)
        {
            IEnumerable<DirectoryInfo> subDirectories = directory.GetDirectories();

            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                ReformatNames(subDirectory);
            }

            NameFormatService nameFormatService = new NameFormatService();
            IEnumerable<FileInfo> videos = GetMatchingFiles(directory, "");
            foreach (FileInfo video in videos)
            {
                nameFormatService.FormatFileName(video);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private DirectoryInfo GetCurrentDirectory(string relativePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(ConfigurationManager.AppSettings["BaseDirectory"], ConfigurationManager.AppSettings["VideosPath"], relativePath));

            return directoryInfo;
        }

        private IEnumerable<FileInfo> GetMatchingFiles(DirectoryInfo directory, string filter)
        {
            FileTypeService fileTypeService = new FileTypeService();

            IEnumerable<FileInfo> validFiles = directory
                .GetFiles()
                .Where(file => fileTypeService.IsFileVideo(file.Name))
                .Where(file => ImageMatchesFilter(file, filter))
                .OrderBy(file => file.Name)
                .ThenBy(file =>
                    {
                        Match numbersInName = Regex.Match(file.Name, "[0-9]+");

                        bool isNumber = int.TryParse(numbersInName.Value, out int numericalMatch);

                        return isNumber ? numericalMatch : 0;
                    }
                );

            return validFiles;
        }

        private bool ImageMatchesFilter(FileInfo fileInfo, string filter)
        {
            if (filter == null)
            {
                return true;
            }

            string[] filters = filter.ToLowerInvariant().Split(',');
            bool isMatch = true;

            // Make sure the file matches all of our filters
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                if (filter.StartsWith("type: "))
                {
                    // Remove the special tag from the filter
                    string filterType = filter.Substring("type: ".Length);

                    isMatch = fileInfo.Extension.Split('.').Last().Equals(filterType, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    isMatch = fileInfo.Name.ToLowerInvariant().Contains(filter.ToLowerInvariant());
                }
            }

            return isMatch;
        }
    }
}