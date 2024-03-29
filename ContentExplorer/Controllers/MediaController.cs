﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using ContentExplorer.Models;
using ContentExplorer.Models.ViewModels;
using ContentExplorer.Services;
using Microsoft.Ajax.Utilities;

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
                return Json(null, JsonRequestBehavior.AllowGet);
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
                return Json(new List<MediaPreviewViewModel>(), JsonRequestBehavior.AllowGet);
            }

            string[] filters = filter.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            IEnumerable<DirectoryInfo> matchingDirectories = GetMatchingSubDirectories(currentDirectoryInfo, filters, mediaType, 0, -1);

            ICollection<MediaPreviewViewModel> directoryPreviews = matchingDirectories
                .Select(subDirectoryInfo =>
                    GetMediaPreviewFromSubDirectory(subDirectoryInfo, hierarchyRootInfo, mediaType)
                )
                .Where(mediaPreview => mediaPreview != null)
                .ToList();

            return Json(directoryPreviews, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult MoveSubDirectories(string[] directoryPaths, string newDirectoryPath, string mediaType)
        {
            directoryPaths = directoryPaths
                .Where(filePath => string.IsNullOrEmpty(filePath) != true)
                .Select(HttpUtility.UrlDecode)
                .ToArray();

            if (directoryPaths.Any() != true || string.IsNullOrEmpty(newDirectoryPath))
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }

            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);

            IThumbnailService thumbnailService = GetThumbnailService(mediaType);

            foreach (string directoryPath in directoryPaths)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo($"{hierarchyRootDiskLocation}\\{directoryPath}");
                DirectoryInfo newDirectoryInfo = new DirectoryInfo($"{hierarchyRootDiskLocation}\\{newDirectoryPath}\\{directoryInfo.Name}");
                if (newDirectoryInfo.Exists != true)
                {
                    newDirectoryInfo.Create();
                }

                IEnumerable<FileInfo> directoryFiles = directoryInfo
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(subFile => thumbnailService.IsThumbnail(subFile) != true);

                foreach (FileInfo fileInfo in directoryFiles)
                {
                    FileInfo thumbnail = thumbnailService.GetFileThumbnail(fileInfo);
                    if (thumbnail.Exists)
                    {
                        string newThumbnailPath = $"{hierarchyRootDiskLocation}\\{newDirectoryPath}\\{directoryInfo.Name}\\{thumbnail.Name}";
                        thumbnail.MoveTo(newThumbnailPath);
                    }

                    if (fileInfo.Exists)
                    {
                        string newFilePath =
                            $"{hierarchyRootDiskLocation}\\{newDirectoryPath}\\{directoryInfo.Name}\\{fileInfo.Name}";
                        fileInfo.MoveTo(newFilePath);
                    }


                }

                TagLink.UpdateDirectoryPath(
                    $"{hierarchyRoot}\\{directoryPath}",
                    $"{hierarchyRoot}\\{newDirectoryPath}\\{directoryInfo.Name}\\"
                );

                bool areRemainingFiles = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Any();
                if (areRemainingFiles != true)
                {
                    directoryInfo.Delete(true);
                }
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult MoveSubFiles(string[] filePaths, string newDirectoryPath, string mediaType)
        {
            filePaths = filePaths
                .Where(filePath => string.IsNullOrEmpty(filePath) != true)
                .Select(HttpUtility.UrlDecode)
                .ToArray();

            if (filePaths.Any() != true || string.IsNullOrEmpty(newDirectoryPath))
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }

            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string hierarchyRoot = GetHierarchyRoot(mediaType);
            string hierarchyRootDiskLocation = Path.Combine(websiteDiskLocation, hierarchyRoot);
            DirectoryInfo newDirectoryInfo = new DirectoryInfo($"{hierarchyRootDiskLocation}\\{newDirectoryPath}");
            if (newDirectoryInfo.Exists != true)
            {
                newDirectoryInfo.Create();
            }

            IThumbnailService thumbnailService = GetThumbnailService(mediaType);
            FileInfo firstFileInfo = new FileInfo($"{hierarchyRootDiskLocation}\\{filePaths[0]}");
            string directoryParentPath = firstFileInfo.DirectoryName;

            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo($"{hierarchyRootDiskLocation}\\{filePath}");
                string newFilePath = $"{hierarchyRootDiskLocation}\\{newDirectoryPath}\\{fileInfo.Name}";

                FileInfo thumbnail = thumbnailService.GetFileThumbnail(fileInfo);
                if (thumbnail.Exists)
                {
                    string newThumbnailPath = $"{hierarchyRootDiskLocation}\\{newDirectoryPath}\\{thumbnail.Name}";
                    thumbnail.MoveTo(newThumbnailPath);
                }

                if (fileInfo.Exists)
                {
                    fileInfo.MoveTo(newFilePath);
                }

                TagLink.UpdateFilePath(
                    $"{hierarchyRoot}\\{filePath}",
                    $"{hierarchyRoot}\\{newDirectoryPath}\\{fileInfo.Name}"
                );
            }

            DirectoryInfo parentDirectory = new DirectoryInfo(directoryParentPath);
            bool areRemainingFiles = parentDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Any();
            if (areRemainingFiles != true)
            {
                parentDirectory.Delete(true);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
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
                return Json(new PaginatedViewModel<MediaPreviewViewModel>()
                {
                    CurrentPage = new List<MediaPreviewViewModel>(),
                    Total = 0
                }, JsonRequestBehavior.AllowGet);
            }

            DirectoryInfo hierarchyRootInfo = new DirectoryInfo(hierarchyRootDiskLocation);
            IEnumerable<FileInfo> subFiles =
                GetMatchingSubFiles(currentDirectoryInfo, filters, mediaType, skip, take, false);

            ICollection<MediaPreviewViewModel> filePreviews = subFiles
                .Select(subFile => GetMediaPreviewFromSubFile(subFile, hierarchyRootInfo, mediaType))
                .ToArray();

            int totalNumberOfFiles = filters.Any() != true
                ? GetDirectoryFilesByMediaType(currentDirectoryInfo, mediaType, false).Count()
                : TagLink.GetFileCount($"{hierarchyRoot}\\{currentDirectory}", filters, skip, take);

            PaginatedViewModel<MediaPreviewViewModel> paginatedViewModel = new PaginatedViewModel<MediaPreviewViewModel>
            {
                CurrentPage = filePreviews,
                Total = totalNumberOfFiles
            };

            return Json(paginatedViewModel, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<FileInfo> GetDirectoryFilesByMediaType(DirectoryInfo directoryInfo, string mediaType,
            bool includeSubDirectories)
        {
            IEnumerable<FileInfo> subFiles = includeSubDirectories
                ? directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories)
                : directoryInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly);

            FileTypeService fileTypeService = new FileTypeService();
            if (mediaType == "image")
            {
                IThumbnailService thumbnailService = new ImageThumbnailService();

                subFiles = subFiles
                    .Where(fileInfo =>
                        fileTypeService.IsFileImage(fileInfo.Name) &&
                        thumbnailService.IsThumbnail(fileInfo) != true
                    );
            }
            else
            {
                subFiles = subFiles
                    .Where(fileInfo =>
                        fileTypeService.IsFileVideo(fileInfo.Name)
                    );
            }

            return subFiles;
        }

        [HttpGet]
        public JsonResult GetSubFile(string currentDirectory, int page, string mediaType, string filter)
        {
            DirectoryInfo currentDirectoryInfo = GetCurrentDirectory(currentDirectory, mediaType);
            string[] filters = filter.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            FileInfo mediaItem = GetMatchingSubFiles(currentDirectoryInfo, filters, mediaType, page - 1, 1, false)
                .FirstOrDefault();

            if (mediaItem == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            DirectoryInfo hierarchicalRootInfo = GetHierarchicalRootInfo(mediaType);
            MediaPreviewViewModel imageViewModel = GetMediaPreviewFromSubFile(mediaItem, hierarchicalRootInfo, mediaType);

            return Json(imageViewModel, JsonRequestBehavior.AllowGet);
        }

        private MediaPreviewViewModel GetMediaPreviewFromSubDirectory(DirectoryInfo subDirectory,
            DirectoryInfo hierarchicalDirectoryInfo, string mediaType)
        {
            IEnumerable<FileInfo> matchingSubFiles = subDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories);
            IEnumerable<FileInfo> orderedSubFiles = OrderAlphabetically(matchingSubFiles);
            FileInfo firstMatchingImage = orderedSubFiles.FirstOrDefault();

            if (firstMatchingImage == null)
            {
                return null;
            }

            IThumbnailService thumbnailService = GetThumbnailService(mediaType);
            thumbnailService.CreateThumbnail(firstMatchingImage);

            MediaPreviewViewModel mediaPreview = new MediaPreviewViewModel
            {
                Name = subDirectory.Name,
                Path = GetUrl(subDirectory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/'),
                ContentUrl = GetUrl(subDirectory),
                ThumbnailUrl = GetUrl(thumbnailService.GetFileThumbnail(firstMatchingImage)),
                TaggingUrl = GetUrl(subDirectory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/')
            };

            return mediaPreview;
        }

        private IThumbnailService GetThumbnailService(string mediaType)
        {
            IThumbnailService thumbnailService;

            switch (mediaType)
            {
                case "image":
                    thumbnailService = new ImageThumbnailService();
                    break;
                case "video":
                    thumbnailService = new VideoThumbnailService();
                    break;
                default:
                    throw new NotImplementedException($"Media type {mediaType} is not supported");
            }

            return thumbnailService;
        }

        private MediaPreviewViewModel GetMediaPreviewFromSubFile(FileInfo subFile,
            DirectoryInfo hierarchicalDirectoryInfo, string mediaType)
        {
            IThumbnailService thumbnailService = GetThumbnailService(mediaType);

            if (thumbnailService.GetFileThumbnail(subFile).Exists != true)
            {
                thumbnailService.CreateThumbnail(subFile);
            }

            MediaPreviewViewModel mediaPreview = new MediaPreviewViewModel
            {
                Name = subFile.Name,
                Path = GetUrl(subFile.Directory).Substring(hierarchicalDirectoryInfo.Name.Length).TrimStart('/'),
                ContentUrl = $"{ConfigurationManager.AppSettings["CDNPath"]}/{GetUrl(subFile)}",
                ThumbnailUrl = GetUrl(thumbnailService.GetFileThumbnail(subFile)),
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

        private IEnumerable<DirectoryInfo> GetMatchingSubDirectories(DirectoryInfo currentDirectoryInfo, string[] filters, string mediaType, int skip, int take)
        {
            IEnumerable<DirectoryInfo> subDirectories =
                currentDirectoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);

            if (filters.Any() != true)
            {
                subDirectories = subDirectories.Skip(skip);

                // Keep the filtering consistent with SQLite
                if (take != -1)
                {
                    subDirectories = subDirectories.Take(take);
                }

                return subDirectories;
            }

            string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];

            IEnumerable<TagLink> directoryTagLinks = subDirectories.SelectMany(subDirectory =>
                TagLink.GetByDirectory(subDirectory.FullName.Substring(cdnDiskLocation.Length + 1), filters, skip, take, true)
            );

            IEnumerable<FileInfo> matchingSubFiles = directoryTagLinks
                .Select(directoryTagLink => new FileInfo($"{cdnDiskLocation}\\{directoryTagLink.FilePath}"));

            IEnumerable<DirectoryInfo> matchingSubDirectories = matchingSubFiles
                .Select(subFile => subFile.Directory)
                .DistinctBy(subFile => subFile.Parent.FullName);

            return matchingSubDirectories;
        }

        private IEnumerable<FileInfo> GetMatchingSubFiles(DirectoryInfo currentDirectoryInfo, string[] filters, string mediaType, int skip, int take, bool includeSubDirectories)
        {
            if (filters.Any() != true)
            {
                IEnumerable<FileInfo> subFiles =
                    GetDirectoryFilesByMediaType(currentDirectoryInfo, mediaType, includeSubDirectories);

                subFiles = subFiles.Skip(skip);

                if (take != -1)
                {
                    subFiles = subFiles.Take(take);
                }

                return subFiles;
            }

            // If we're filtering the files, any files without a tag will be skipped anyway. We may as well go solely off the tag links
            string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string directoryPath = currentDirectoryInfo.FullName.Substring(cdnDiskLocation.Length + 1);
            IEnumerable<TagLink> directoryTagLinks = TagLink.GetByDirectory(directoryPath, filters, skip, take);
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