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
            DirectoryInfo currentDirectoryInfo = GetCurrentDirectory(currentDirectory, mediaType);

            if (!currentDirectoryInfo.Exists)
            {
                throw new InvalidOperationException("Current directory could not be found");
            }

            DirectoryInfo hierarchyRootInfo = GetHierarchicalRootInfo(mediaType);
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
                .Where(subDirectoryInfo => GetMatchingSubFiles(subDirectoryInfo, mediaType, filter, true).Any())
                .Select(subDirectoryInfo => GetMediaPreviewFromSubDirectory(subDirectoryInfo, hierarchyRootInfo, mediaType, filter))
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

            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            IEnumerable<FileInfo> subFiles = GetMatchingSubFiles(currentDirectoryInfo, mediaType, filter, false);
            ICollection<MediaPreviewViewModel> filePreviews = OrderAlphabetically(subFiles)
                .Skip(50 * (page - 1))
                .Take(50)
                .Select(subFile => GetMediaPreviewFromSubFile(subFile, hierarchyRootInfo))
                .ToArray();

            PaginatedViewModel<MediaPreviewViewModel> paginatedViewModel = new PaginatedViewModel<MediaPreviewViewModel>
            {
                CurrentPage = filePreviews,
                Total = subFiles.Count()
            };

            return Json(paginatedViewModel, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSubFile(string currentDirectory, int page, string mediaType, string filter)
        {
            DirectoryInfo currentDirectoryInfo = GetCurrentDirectory(currentDirectory, mediaType);
            IEnumerable<FileInfo> fileInfos = GetMatchingSubFiles(currentDirectoryInfo, mediaType, filter, false);
            FileInfo image = OrderAlphabetically(fileInfos).ElementAt(page - 1);

            DirectoryInfo hierarchicalRootInfo = GetHierarchicalRootInfo(mediaType);
            MediaPreviewViewModel imageViewModel = GetMediaPreviewFromSubFile(image, hierarchicalRootInfo);

            return Json(imageViewModel, JsonRequestBehavior.AllowGet);
        }

        private MediaPreviewViewModel GetMediaPreviewFromSubDirectory(DirectoryInfo subDirectory, DirectoryInfo hierarchicalDirectoryInfo, string mediaType, string filter)
        {
            IEnumerable<FileInfo> matchingSubFiles = GetMatchingSubFiles(subDirectory, mediaType, filter, true);
            IEnumerable<FileInfo> orderedSubFiles = OrderAlphabetically(matchingSubFiles);
            FileInfo firstMatchingImage = orderedSubFiles.First();

            MediaPreviewViewModel mediaPreview = new MediaPreviewViewModel
            {
                Name = subDirectory.Name,
                Path = GetUrl(subDirectory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/'),
                ContentUrl = GetUrl(subDirectory),
                ThumbnailUrl = GetUrl(firstMatchingImage),
                TaggingUrl = GetUrl(subDirectory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/')
            };

            return mediaPreview;
        }

        private MediaPreviewViewModel GetMediaPreviewFromSubFile(FileInfo subFile, DirectoryInfo hierarchicalDirectoryInfo)
        {
            MediaPreviewViewModel mediaPreview = new MediaPreviewViewModel
            {
                Name = subFile.Name,
                Path = GetUrl(subFile.Directory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/'),
                ContentUrl = $"{ConfigurationManager.AppSettings["CDNPath"]}/{GetUrl(subFile)}",
                ThumbnailUrl = GetUrl(subFile),
                TaggingUrl = GetUrl(subFile).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/'),
            };

            return mediaPreview;
        }

        private IEnumerable<TFilesystemInfo> OrderAlphabetically<TFilesystemInfo>(IEnumerable<TFilesystemInfo> unorderedEnumerable) where TFilesystemInfo : FileSystemInfo
        {
            IEnumerable<TFilesystemInfo> orderedEnumerable = unorderedEnumerable
                .OrderBy(subFile => subFile.FullName)
                .ThenBy(file =>
                {
                    Match numbersInName = Regex.Match(file.Name, "[0-9]+");

                    bool isNumber = int.TryParse(numbersInName.Value, out int numericalMatch);

                    return isNumber ? numericalMatch : 0;
                });

            return orderedEnumerable;
        }

        private IEnumerable<FileInfo> GetMatchingSubFiles(DirectoryInfo currentDirectoryInfo, string mediaType, string filter, bool includeSubDirectories)
        {
            FileTypeService fileTypeService = new FileTypeService();
            IEnumerable<FileInfo> subFiles = includeSubDirectories ?
                currentDirectoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories) :
                currentDirectoryInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly);

            if (mediaType == "image")
            {
                subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileImage(fileInfo.Name));
            }
            else
            {
                subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileVideo(fileInfo.Name));
            }

            IFileSystemFilteringService fileSytemFilteringService = new FileSystemFilteringService();
            subFiles = subFiles.Where(fileInfo => fileSytemFilteringService.FileMatchesFilter(fileInfo, filter)).ToList();

            return subFiles;
        }


        private DirectoryInfo GetCurrentDirectory(string relativePath, string mediaType)
        {
            DirectoryInfo hierarchicalRootInfo = GetHierarchicalRootInfo(mediaType);
            string currentDirectoryPath = Path.Combine(hierarchicalRootInfo.FullName, relativePath);
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(currentDirectoryPath);

            return currentDirectoryInfo;
        }

        private DirectoryInfo GetHierarchicalRootInfo(string mediaType)
        {
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);
            DirectoryInfo hierarchicalRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);

            return hierarchicalRootInfo;
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

        private string GetUrl(FileSystemInfo fileSystemInfo)
        {
            string rawUrl = fileSystemInfo.FullName.Substring(ConfigurationManager.AppSettings["BaseDirectory"].Length + 1);
            string encodedUrl = rawUrl
                .Replace("'", "%27")
                .Replace("\\", "/")
                .Replace("#", "%23");

            return encodedUrl;
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