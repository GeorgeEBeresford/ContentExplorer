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
    public class MediaController : Controller
    {
        [HttpGet]
        public JsonResult GetDirectoryHierarchy(string currentDirectory, string mediaType)
        {
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);

            string currentDirectoryPath = Path.Combine(hierarchyRootDiskLocation, currentDirectory);
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(currentDirectoryPath);

            if (!currentDirectoryInfo.Exists)
            {
                throw new InvalidOperationException("Current directory could not be found");
            }

            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            DirectoryHierarchyViewModel directoryHierarchy =
                GetDirectoryAsHierarchy(currentDirectoryInfo, hierarchyRootInfo);

            return Json(directoryHierarchy, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSubDirectories(string currentDirectory, string mediaType, string filter)
        {
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);

            string currentDirectoryPath = Path.Combine(hierarchyRootDiskLocation, currentDirectory);
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(currentDirectoryPath);

            if (!currentDirectoryInfo.Exists)
            {
                throw new InvalidOperationException("Current directory could not be found");
            }

            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            DirectoryInfo[] subDirectoryInfos = currentDirectoryInfo.GetDirectories();
            ICollection<MediaPreviewViewModel> directoryPreviews = subDirectoryInfos
                .Where(subDirectoryInfo =>
                    subDirectoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories)
                    .Any(subFile => FileMatchesFilter(subFile, filter, mediaType))
                )
                .Select(subDirectoryInfo => new MediaPreviewViewModel
                {
                    Name = subDirectoryInfo.Name,
                    Path = GetUrl(subDirectoryInfo).Substring(hierarchyRootInfo.Name.Length).TrimStart('/'),
                    ContentUrl = GetUrl(subDirectoryInfo),
                    ThumbnailUrl = GetUrl(subDirectoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).First()),
                    TaggingUrl = GetUrl(subDirectoryInfo).Substring(hierarchyRootInfo.Name.Length).TrimStart('/')
                })
                .ToList();

            return Json(directoryPreviews, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSubFiles(string currentDirectory, string mediaType, int page, string filter)
        {
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);

            string currentDirectoryPath = Path.Combine(hierarchyRootDiskLocation, currentDirectory);
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(currentDirectoryPath);

            if (!currentDirectoryInfo.Exists)
            {
                throw new InvalidOperationException("Current directory could not be found");
            }

            FileTypeService fileTypeService = new FileTypeService();
            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            IEnumerable<FileInfo> subFiles = currentDirectoryInfo.EnumerateFiles();

            if (mediaType == "image")
            {
                subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileImage(fileInfo.Name));
            }
            else
            {
                subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileVideo(fileInfo.Name));
            }

            subFiles = subFiles.Where(fileInfo => FileMatchesFilter(fileInfo, filter, mediaType)).ToList();

            ICollection<MediaPreviewViewModel> filePreviews = subFiles
                .OrderBy(subFile => subFile.Name)
                .ThenBy(file =>
                {
                    Match numbersInName = Regex.Match(file.Name, "[0-9]+");

                    bool isNumber = int.TryParse(numbersInName.Value, out int numericalMatch);

                    return isNumber ? numericalMatch : 0;
                })
                .Skip(50 * (page - 1))
                .Take(50)
                .Select(subFile => new MediaPreviewViewModel
                {
                    Name = subFile.Name,
                    Path = GetUrl(subFile.Directory).Substring(hierarchyRootInfo.Name.Length).TrimStart('/'),
                    ContentUrl = GetUrl(subFile),
                    ThumbnailUrl = GetUrl(subFile),
                    TaggingUrl = GetUrl(subFile).Substring(hierarchyRootInfo.Name.Length).TrimStart('/'),
                })
                .ToArray();

            PaginatedViewModel<MediaPreviewViewModel> paginatedViewModel = new PaginatedViewModel<MediaPreviewViewModel>
            {
                CurrentPage = filePreviews,
                Total = subFiles.Count()
            };

            return Json(paginatedViewModel, JsonRequestBehavior.AllowGet);
        }

        private bool FileMatchesFilter(FileInfo fileInfo, string filterString, string mediaType)
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

        private DirectoryHierarchyViewModel GetDirectoryAsHierarchy(DirectoryInfo directoryInfo,
            DirectoryInfo hierarchyRootInfo)
        {
            DirectoryHierarchyViewModel directoryWithParents = new DirectoryHierarchyViewModel
            {
                Name = directoryInfo.Name,
                ContentUrl = GetUrl(directoryInfo),
                Path = GetUrl(directoryInfo).Substring(hierarchyRootInfo.Name.Length).TrimStart('/')
            };

            if (directoryInfo.Parent != null && directoryInfo.Parent.FullName != hierarchyRootInfo.Parent?.FullName)
            {
                directoryWithParents.Parent = GetDirectoryAsHierarchy(directoryInfo.Parent, hierarchyRootInfo);
            }

            return directoryWithParents;
        }

        private string GetUrl(FileSystemInfo directoryInfo)
        {
            return directoryInfo.FullName.Substring(ConfigurationManager.AppSettings["BaseDirectory"].Length + 1).Replace("\\", "/");
        }

        private string GetHierarchyRoot(string mediaType)
        {
            mediaType = mediaType.ToLowerInvariant();

            switch (mediaType)
            {
                case "image":
                    return ConfigurationManager.AppSettings["ImagesPath"];
                case "video":
                    return ConfigurationManager.AppSettings["VideosPath"];
                default:
                    Response.StatusCode = 422;
                    throw new InvalidOperationException("Media type is not supported");
            }
        }
    }
}