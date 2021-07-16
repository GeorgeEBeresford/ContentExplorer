using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ContentExplorer.Models;
using ContentExplorer.Services;

namespace ContentExplorer.Controllers
{
    public class VideoController : Controller
    {
        [HttpGet]
        public ActionResult Index(string path = "", int? page = 1, string filter = null)
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

            if (directoryInfo.Exists == false)
            {
                return RedirectToAction("Index", new { page, filter });
            }

            ViewBag.FilesPerPage = 50;
            ViewBag.Directory = directoryInfo;
            ViewBag.Page = page;
            ViewBag.Filter = filter;

            return View();
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
            ViewBag.Path = path;
            ViewBag.Id = page;
            ViewBag.Filter = filter;

            // Zero-based index
            int pageIndex = page - 1;

            int startingPreview = pageIndex - 7 < 1 ? 1 : page - 7;
            ViewBag.StartingPreview = startingPreview;

            return View();
        }

        [HttpGet]
        public ActionResult RebuildThumbnails(string path = "")
        {
            IThumbnailService videoThumbnailService = new VideoThumbnailService();

            DirectoryInfo baseDirectory = GetCurrentDirectory(path);
            IEnumerable<FileInfo> videos = baseDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(image => videoThumbnailService.IsThumbnail(image) != true);

            foreach (FileInfo video in videos)
            {
                videoThumbnailService.DeleteThumbnailIfExists(video);
                videoThumbnailService.CreateThumbnail(video);
            }

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

        private ActionResult ReformatNames(DirectoryInfo directory)
        {
            IEnumerable<DirectoryInfo> subDirectories = directory.GetDirectories();

            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                ReformatNames(subDirectory);
            }

            NameFormatService nameFormatService = new NameFormatService();
            IEnumerable<FileInfo> videos = directory.EnumerateFiles("*", SearchOption.AllDirectories);
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
    }
}