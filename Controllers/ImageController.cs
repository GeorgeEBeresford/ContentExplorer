using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ContentExplorer.Models;
using ContentExplorer.Models.ViewModels;
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
            ICollection<FileInfo> validFiles = GetOrderedFiles(directoryInfo, filter).ToList();

            int imagesPerPage = 50;
            ViewBag.FilesPerPage = imagesPerPage;

            FileTypeService fileTypeService = new FileTypeService();

            ICollection<DirectoryInfo> subDirectories = directoryInfo
                .EnumerateDirectories()
                .Where(directory =>
                    directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Any(file =>
                            fileTypeService.IsFileImage(file.Name) && ImageMatchesFilter(file, filter)
                        )
                )
                .ToList();

            DirectoryViewModel imagesViewModel = new DirectoryViewModel
            {
                FileInfos = validFiles
                    .Skip(imagesPerPage * (page.Value - 1)).Take(imagesPerPage)
                    .ToList(),
                FileCount = validFiles.Count(),
                DirectoryInfos = subDirectories
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
            FileInfo image = GetOrderedFiles(directoryInfo, filter).ElementAt(page - 1);

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
            ICollection<FileInfo> validFiles = GetOrderedFiles(currentDirectory, filter)
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

        private DirectoryInfo GetCurrentDirectory(string relativePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(ConfigurationManager.AppSettings["BaseDirectory"], ConfigurationManager.AppSettings["ImagesPath"], relativePath));

            return directoryInfo;
        }

        private IEnumerable<FileInfo> GetOrderedFiles(DirectoryInfo directory, string filter)
        {
            FileTypeService fileTypeService = new FileTypeService();

            IEnumerable<FileInfo> validFiles = directory
                .EnumerateFiles()
                .Where(file =>
                    fileTypeService.IsFileImage(file.Name) && ImageMatchesFilter(file, filter)
                );

            IEnumerable<FileInfo> orderedfiles = validFiles
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

            return orderedfiles;
        }

        private bool ImageMatchesFilter(FileInfo fileInfo, string filterString)
        {
            if (string.IsNullOrEmpty(filterString))
            {
                return true;
            }

            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string filePath = fileInfo.FullName.Substring(websiteDiskLocation.Length + 1);

            string[] filters = filterString.ToLowerInvariant().Split(',');
            bool isMatch = true;

            // Make sure the file matches all of our filters
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                string filter = filters[filterIndex];

                if (filter == "")
                {
                    isMatch = true;
                }
                else if (filter.StartsWith("type:"))
                {
                    // Remove the special tag from the filter
                    string filterType = filter.Substring("type:".Length).Trim();

                    isMatch = fileInfo.Extension.Split('.').Last().Equals(filterType, StringComparison.OrdinalIgnoreCase);
                }
                else if (filter.StartsWith("name:"))
                {
                    // Remove the special tag from the filter
                    string filterName = filter.Substring("name:".Length).Trim();

                    isMatch = fileInfo.Name.ToLowerInvariant().Contains(filterName.ToLowerInvariant());
                }
                else
                {
                    isMatch = Tag.GetByFile(filePath).Any(tag => tag.TagName.Equals(filter, StringComparison.OrdinalIgnoreCase));
                }
            }

            return isMatch;
        }
    }
}