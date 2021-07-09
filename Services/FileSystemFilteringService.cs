using ContentExplorer.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace ContentExplorer.Services
{
    public class FileSystemFilteringService : IFileSystemFilteringService
    {
        public bool FileMatchesFilter(FileInfo fileInfo, string[] filters)
        {
            if (filters.Any() != true)
            {
                return true;
            }

            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string filePath = fileInfo.FullName.Substring(websiteDiskLocation.Length + 1);

            bool isMatch = true;

            // Make sure the file matches all of our filters
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                string filter = filters[filterIndex];

                bool? fileMatchesSpecialFilter = FileMatchesSpecialFilter(fileInfo, filter);

                if (fileMatchesSpecialFilter.HasValue)
                {
                    return fileMatchesSpecialFilter.Value;
                }

                IEnumerable<Tag> tagsForFile = Tag.GetByFile(filePath);
                isMatch = tagsForFile.Any(tag =>
                    tag.TagName.Equals(filter, StringComparison.OrdinalIgnoreCase)
                );
            }

            return isMatch;
        }

        public bool TagLinkMatchesFilter(IEnumerable<TagLink> tagLinksForFile, string[] filters)
        {
            if (filters.Any() != true)
            {
                return true;
            }

            tagLinksForFile = tagLinksForFile.ToList();

            if (tagLinksForFile.Any() != true)
            {
                return false;
            }

            bool isMatch = true;
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string filePath = tagLinksForFile.First().FilePath;
            FileInfo fileInfo = new FileInfo($"{websiteDiskLocation}\\{filePath}");

            // Make sure the file matches all of our filters
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                string filter = filters[filterIndex];

                bool? fileMatchesSpecialFilter = FileMatchesSpecialFilter(fileInfo, filter);

                if (fileMatchesSpecialFilter.HasValue)
                {
                    return fileMatchesSpecialFilter.Value;
                }

                IEnumerable<string> tagsForFile = tagLinksForFile
                    .GroupBy(tagLink => tagLink.GetTag().TagName)
                    .Select(tagGrouping => tagGrouping.Key);

                isMatch = tagsForFile.Any(tagName =>
                    tagName.Equals(filter, StringComparison.OrdinalIgnoreCase)
                );
            }

            return isMatch;
        }

        public bool DirectoryMatchesFilter(DirectoryInfo directoryInfo, string[] filters, string mediaType)
        {
            string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string directoryPath = directoryInfo.FullName.Substring(cdnDiskLocation.Length + 1);
            IEnumerable<FileInfo> subFiles = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories);
            FileTypeService fileTypeService = new FileTypeService();

            if (mediaType == "image")
            {
                subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileImage(fileInfo.Name));
            }
            else
            {
                subFiles = subFiles.Where(fileInfo => fileTypeService.IsFileVideo(fileInfo.Name));
            }

            // Check for any special filters
            bool directoryContainsMatchingFile = subFiles
                .Any(subfile =>
                    filters.Any(filter =>

                        FileMatchesSpecialFilter(subfile, filter) ?? false
                    )
                );

            if (directoryContainsMatchingFile)
            {
                return true;
            }

            ICollection<TagLink> tagLinksForDirectory = TagLink.GetByDirectory(directoryPath);
            directoryContainsMatchingFile = TagLinkMatchesFilter(tagLinksForDirectory, filters);

            return directoryContainsMatchingFile;
        }

        private bool? FileMatchesSpecialFilter(FileInfo fileInfo, string filter)
        {
            if (filter.StartsWith("type:"))
            {
                // Remove the special tag from the filter
                string filterType = filter.Substring("type:".Length).Trim();

                return fileInfo.Extension.Split('.').Last().Equals(filterType, StringComparison.OrdinalIgnoreCase);
            }
            else if (filter.StartsWith("name:"))
            {
                // Remove the special tag from the filter
                string filterName = filter.Substring("name:".Length).Trim();

                return fileInfo.Name.ToLowerInvariant().Contains(filterName.ToLowerInvariant());
            }

            return null;
        }
    }
}