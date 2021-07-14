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

                IEnumerable<Tag> tagsForFile = Tag.GetByFile(filePath, filters);
                isMatch = tagsForFile.Any(tag =>
                    tag.TagName.Equals(filter, StringComparison.OrdinalIgnoreCase)
                );
            }

            return isMatch;
        }

        public bool TagsMatchFilter(IEnumerable<Tag> tags, IEnumerable<string> filters)
        {
            bool tagsMatchFilter = filters.All(filter =>
                tags.Any(tag =>
                    filter.Equals(tag.TagName, StringComparison.OrdinalIgnoreCase)
                )
            );

            return tagsMatchFilter;
        }

        public bool TagLinkMatchesFilter(IEnumerable<TagLink> tagLinks, string[] filters)
        {
            if (filters.Any() != true)
            {
                return true;
            }

            string[] tagsForFile = tagLinks
                .GroupBy(tagLink => tagLink.GetTag().TagName)
                .Select(tagGrouping => tagGrouping.Key)
                .ToArray();

            if (tagsForFile.Any() != true)
            {
                return false;
            }

            // Make sure the file matches all of our filters
            bool isMatch = true;
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                string filter = filters[filterIndex];

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
            ICollection<Tag> directoryTags = Tag.GetByDirectory(directoryPath, filters, true);
            bool directoryContainsMatchingFile = directoryTags.Any();

            return directoryContainsMatchingFile;
        }
    }
}