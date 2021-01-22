using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Services;
using System.Web.UI.WebControls;
using ContentExplorer.Models.ViewModels;
using ContentExplorer.Services;

namespace ContentExplorer.Controllers
{
    public class ImageController : Controller
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

            int imagesPerPage = 50;
            ViewBag.ImagesPerPage = imagesPerPage;

            DirectoryViewModel imagesViewModel = new DirectoryViewModel
            {
                FileInfos = validFiles
                    .Skip(imagesPerPage * (page.Value - 1)).Take(imagesPerPage)
                    .ToList(),
                FileCount = validFiles.Count(),
                DirectoryInfos = directoryInfo.GetDirectories()
            };

            ViewBag.Directory = directoryInfo;
            ViewBag.Page = page;
            ViewBag.Filter = filter;

            return View(imagesViewModel);
        }

        [HttpGet]
        public JsonResult GetImagePath(string directoryPath, int page, string filter = "")
        {
            if (filter == null)
            {
                filter = "";
            }

            string baseDirectory = ConfigurationManager.AppSettings["ImagesPath"];
            string cdn = ConfigurationManager.AppSettings["CDNPath"];

            DirectoryInfo directoryInfo = GetCurrentDirectory(directoryPath);
            FileInfo image = GetMatchingFiles(directoryInfo, filter).ElementAt(page - 1);

            string imageWebPath = Path.Combine(cdn, baseDirectory, directoryPath, image.Name)
                .Replace("'", "%27")
                .Replace("\\", "/");

            ImageViewModel imageViewModel = new ImageViewModel
            {
                ImageName = image.Name,
                ImagePath = imageWebPath
            };

            return Json(imageViewModel, JsonRequestBehavior.AllowGet);
        }

        private DirectoryInfo GetCurrentDirectory(string relativePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(ConfigurationManager.AppSettings["BaseDirectory"], ConfigurationManager.AppSettings["ImagesPath"], relativePath));

            return directoryInfo;
        }

        private IEnumerable<FileInfo> GetMatchingFiles(DirectoryInfo directory, string filter)
        {
            FileTypeService fileTypeService = new FileTypeService();

            IEnumerable<FileInfo> validFiles = directory
                .GetFiles()
                .Where(file => fileTypeService.IsFileImage(file.Name))
                .Where(file => ImageMatchesFilter(file, filter))
                .Select(file => new
                {
                    File = file,
                    Numerics = Regex.Match(file.Name, "[0-9]+")
                })
                .OrderBy(fileWithNumerics =>
                {
                    int.TryParse(fileWithNumerics.Numerics.Value, out int numericalMatch);

                    return numericalMatch;
                })
                .Select(fileWithNumerics => fileWithNumerics.File);

            return validFiles;
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

            ViewBag.Image = firstFile;
            ViewBag.ImageCount = fileCount;
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

        private bool ImageMatchesFilter(FileInfo fileInfo, string filter)
        {
            if (filter == null)
            {
                return true;
            }

            string[] filters = filter.ToLowerInvariant().Split('&');
            bool isMatch = true;

            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                switch (filter)
                {
                    case "gif":
                    isMatch = fileInfo.Extension.Split('.').Last().Equals("gif", StringComparison.OrdinalIgnoreCase);
                    break;
                }
            }

            return isMatch;
        }
    }
}