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
        public bool FileMatchesFilter(FileInfo fileInfo, string filterString)
        {
            if (string.IsNullOrEmpty(filterString))
            {
                return true;
            }

            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string filePath = fileInfo.FullName.Substring(websiteDiskLocation.Length + 1);

            bool isMatch = true;
            string[] filters = GetFiltersFromString(filterString);

            // Make sure the file matches all of our filters
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                string filter = filters[filterIndex];

                if (filter.StartsWith("type:"))
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
                    isMatch = Tag.GetByFile(filePath).Any(tag =>
                        tag.TagName.Equals(filter, StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            return isMatch;
        }

        public bool FileMatchesFilter(IEnumerable<TagLink> tagLinksForFile, string filterString)
        {
            bool isMatch = true;
            string[] filters = GetFiltersFromString(filterString);
            string websiteDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string filePath = tagLinksForFile.First().FilePath;
            FileInfo fileInfo = new FileInfo($"{websiteDiskLocation}\\{filePath}");

            // Make sure the file matches all of our filters
            for (int filterIndex = 0; filterIndex < filters.Length && isMatch; filterIndex++)
            {
                string filter = filters[filterIndex];

                if (filter.StartsWith("type:"))
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
                    isMatch = tagLinksForFile.Any(tagLinkForFile =>
                        tagLinkForFile.GetTag().TagName.Equals(filter, StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            return isMatch;
        }

        private string[] GetFiltersFromString(string filterString)
        {
            string[] filters = filterString.ToLowerInvariant().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return filters;
        }
    }
}