using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ContentExplorer.Models;
using ContentExplorer.Services;

namespace ContentExplorer.Controllers
{
    public class ImageController : Controller
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
        public ActionResult ReformatNames(string path = "")
        {
            DirectoryInfo baseDirectory = GetCurrentDirectory(path);
            IEnumerable<FileInfo> subFiles = baseDirectory.EnumerateFiles("*", SearchOption.AllDirectories);
            NameFormatService nameFormatService = new NameFormatService();
            string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];

            foreach (FileInfo subFile in subFiles)
            {
                FileInfo newFileName = nameFormatService.FormatFileName(subFile);

                if (subFile.Name.Equals(newFileName.Name, StringComparison.OrdinalIgnoreCase) != true)
                {
                    ICollection<TagLink> tagLinksForFile = TagLink.GetByFileName(subFile.FullName);

                    foreach (TagLink tagLink in tagLinksForFile)
                    {
                        tagLink.FilePath = newFileName.FullName.Substring(cdnDiskLocation.Length + 1);
                        tagLink.Update();
                    }
                }
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult RebuildThumbnails(string path = "")
        {
            IThumbnailService imageThumbnailService = new ImageThumbnailService();

            DirectoryInfo baseDirectory = GetCurrentDirectory(path);
            IEnumerable<FileInfo> images = baseDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(image => imageThumbnailService.IsThumbnail(image) != true);
            foreach (FileInfo image in images)
            {
                imageThumbnailService.DeleteThumbnailIfExists(image);
                imageThumbnailService.CreateThumbnail(image);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
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

            return View();
        }

        private DirectoryInfo GetCurrentDirectory(string relativePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(ConfigurationManager.AppSettings["BaseDirectory"], ConfigurationManager.AppSettings["ImagesPath"], relativePath));

            return directoryInfo;
        }
    }
}