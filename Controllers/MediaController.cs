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

            string[] filters = filter.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            IEnumerable<DirectoryInfo> matchingDirectories = GetMatchingSubDirectories(currentDirectoryInfo, mediaType, filters);

            ICollection<MediaPreviewViewModel> directoryPreviews = matchingDirectories
                .Select(subDirectoryInfo =>
                    GetMediaPreviewFromSubDirectory(subDirectoryInfo, hierarchyRootInfo)
                )
                .Where(mediaPreview => mediaPreview != null)
                .ToList();

            return Json(directoryPreviews, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSubFiles(string currentDirectory, string mediaType, string filter, int skip, int take)
        {
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);
            string[] filters = filter.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            string currentDirectoryPath = Path.Combine(hierarchyRootDiskLocation, currentDirectory);
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(currentDirectoryPath);

            if (!currentDirectoryInfo.Exists)
            {
                throw new InvalidOperationException("Current directory could not be found");
            }

            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            ICollection<FileInfo> subFiles = GetMatchingSubFiles(currentDirectoryInfo, mediaType, filters, false)
                .ToList();

            ICollection<MediaPreviewViewModel> filePreviews = subFiles
                .Skip(skip)
                .Take(take)
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
            string[] filters = filter.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<FileInfo> fileInfos = GetMatchingSubFiles(currentDirectoryInfo, mediaType, filters, false);
            FileInfo image = fileInfos.ElementAt(page - 1);

            DirectoryInfo hierarchicalRootInfo = GetHierarchicalRootInfo(mediaType);
            MediaPreviewViewModel imageViewModel = GetMediaPreviewFromSubFile(image, hierarchicalRootInfo);

            return Json(imageViewModel, JsonRequestBehavior.AllowGet);
        }

        private MediaPreviewViewModel GetMediaPreviewFromSubDirectory(DirectoryInfo subDirectory,
            DirectoryInfo hierarchicalDirectoryInfo)
        {
            IEnumerable<FileInfo> matchingSubFiles = subDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories);
            IEnumerable<FileInfo> orderedSubFiles = OrderAlphabetically(matchingSubFiles);
            FileInfo firstMatchingImage = orderedSubFiles.FirstOrDefault();

            if (firstMatchingImage == null)
            {
                return null;
            }

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

        private MediaPreviewViewModel GetMediaPreviewFromSubFile(FileInfo subFile,
            DirectoryInfo hierarchicalDirectoryInfo)
        {
            MediaPreviewViewModel mediaPreview = new MediaPreviewViewModel
            {
                Name = subFile.Name,
                Path = GetUrl(subFile.Directory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/'),
                ContentUrl = $"{ConfigurationManager.AppSettings["CDNPath"]}/{GetUrl(subFile)}",
                ThumbnailUrl = GetUrl(subFile),
                TaggingUrl = GetUrl(subFile).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/')
            };

            return mediaPreview;
        }

        private IEnumerable<TFilesystemInfo> OrderAlphabetically<TFilesystemInfo>(
            IEnumerable<TFilesystemInfo> unorderedEnumerable) where TFilesystemInfo : FileSystemInfo
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

        private IEnumerable<DirectoryInfo> GetMatchingSubDirectories(DirectoryInfo currentDirectoryInfo, string mediaType,
            string[] filters)
        {
            IEnumerable<DirectoryInfo> subDirectories =
                currentDirectoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);

            if (filters.Any() != true)
            {
                return subDirectories;
            }

            IEnumerable<DirectoryInfo> matchingSubDirectories = subDirectories
                .Where(subDirectory =>
                {
                    string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
                    string directoryPath = subDirectory.FullName.Substring(cdnDiskLocation.Length + 1);
                    IEnumerable<TagLink> directoryTagLinks = TagLink.GetByDirectory(directoryPath, filters, true);
                    return directoryTagLinks.Any();
                });

            return matchingSubDirectories;
        }

        private IEnumerable<FileInfo> GetMatchingSubFiles(DirectoryInfo currentDirectoryInfo, string mediaType,
            string[] filters, bool includeSubDirectories)
        {
            if (filters.Any() != true)
            {
                FileTypeService fileTypeService = new FileTypeService();
                IEnumerable<FileInfo> subFiles = includeSubDirectories
                    ? currentDirectoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories)
                    : currentDirectoryInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly);

                if (mediaType == "image")
                {
                    subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileImage(fileInfo.Name));
                }
                else
                {
                    subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileVideo(fileInfo.Name));
                }

                return subFiles;
            }

            // If we're filtering the files, any files without a tag will be skipped anyway. We may as well go solely off the tag links
            string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string directoryPath = currentDirectoryInfo.FullName.Substring(cdnDiskLocation.Length + 1);
            IEnumerable<TagLink> directoryTagLinks = TagLink.GetByDirectory(directoryPath, filters);
            IEnumerable<FileInfo> filesFromTagLinks =
                directoryTagLinks.Select(tagLink => new FileInfo($"{cdnDiskLocation}\\{tagLink.FilePath}"));

            return filesFromTagLinks;
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
            string rawUrl =
                fileSystemInfo.FullName.Substring(ConfigurationManager.AppSettings["BaseDirectory"].Length + 1);
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