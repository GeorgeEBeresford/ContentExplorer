using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ContentExplorer.Models;
using ContentExplorer.Services;

namespace ContentExplorer.Controllers
{
    public class TagController : Controller
    {
        [HttpPost]
        public JsonResult AddTagsToFiles(string[] filePaths, string[] tags, string mediaType)
        {
            // Filter inputs that would make no sense
            filePaths = filePaths.Where(filePath => filePath != "").ToArray();
            tags = tags.Where(tag => tag != "").Select(tag => tag.Trim()).ToArray();

            string rootDirectory = GetRootDirectory(mediaType);
            IEnumerable<string> diskLocations = filePaths
                .Select(filePath => Path.Combine(rootDirectory, filePath))
                .ToArray();

            bool isSuccess = AddTags(diskLocations, tags);

            return Json(isSuccess);
        }

        [HttpPost]
        public JsonResult AddTagsToDirectories(string[] directoryPaths, string[] tags, string mediaType)
        {
            // Filter inputs that would make no sense
            directoryPaths = directoryPaths
                .Where(filePath => filePath != "")
                .Select(HttpUtility.UrlDecode)
                .ToArray();

            tags = tags.Where(tag => tag != "").Select(tag => tag.Trim()).ToArray();

            string cdnDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            string rootDirectory = Path.Combine(cdnDiskLocation, GetRootDirectory(mediaType));
            directoryPaths = directoryPaths.Select(directoryPath => Path.Combine(rootDirectory, directoryPath)).ToArray();

            bool isSuccess = true;
            foreach (string directoryPath in directoryPaths)
            {
                DirectoryInfo directory = new DirectoryInfo(directoryPath);
                if (directory.Exists)
                {
                    IEnumerable<string> fileNames = directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Select(subFile => subFile.FullName.Substring(cdnDiskLocation.Length + 1));

                    isSuccess &= AddTags(fileNames, tags);
                }
            }

            return Json(isSuccess);
        }

        [HttpGet]
        public JsonResult GetDirectoryTags(string directoryName, string mediaType, string filter)
        {
            directoryName = GetRootDirectory(mediaType) + "\\" + directoryName;

            string[] filters = filter.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            IFileSystemFilteringService fileSystemFilteringService = new FileSystemFilteringService();
            ICollection<TagLink> tagLinks = TagLink.GetByDirectory(directoryName);

            var tagLinksGroupedByFile = tagLinks.GroupBy(tagLink => tagLink.FilePath);

            var filteredTagLinks = tagLinksGroupedByFile
                .Where(tagsLinksForFile =>
                    fileSystemFilteringService.TagLinkMatchesFilter(tagsLinksForFile, filters)
                ).ToList();

            IEnumerable<Tag> tagsForDirectory = filteredTagLinks
                .SelectMany(tagLinksForFile =>
                    tagLinksForFile.Select(tagLinkForFile =>
                        tagLinkForFile.GetTag()
                    )
                )
                .GroupBy(tag => tag.TagName)
                .Select(grouping => grouping.First())
                .OrderBy(tag => tag.TagName)
                .ToArray();

            return Json(tagsForDirectory, JsonRequestBehavior.AllowGet);
        }

        private string GetRootDirectory(string mediaType)
        {
            if (mediaType.Equals("video", StringComparison.OrdinalIgnoreCase))
            {
                return ConfigurationManager.AppSettings["VideosPath"];
            }
            else if (mediaType.Equals("image", StringComparison.OrdinalIgnoreCase))
            {
                return ConfigurationManager.AppSettings["ImagesPath"];
            }
            else
            {
                throw new InvalidOperationException("Media type is unsupported.");
            }
        }

        public JsonResult DeleteAllTags()
        {
            IEnumerable<Tag> tags = Tag.GetAll();

            foreach (Tag tag in tags)
            {
                IEnumerable<TagLink> tagLinks = TagLink.GetByTagName(tag.TagName);
                foreach (TagLink tagLink in tagLinks)
                {
                    tagLink.Delete();
                }

                tag.Delete();
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult DeleteUnusedTags()
        {
            IEnumerable<TagLink> allTagLinks = TagLink.GetAll();
            string cdnDiskPath = ConfigurationManager.AppSettings["BaseDirectory"];

            IEnumerable<TagLink> unusedTagLinks = allTagLinks
                .Where(tagLink => new FileInfo($"{cdnDiskPath}\\{tagLink.FilePath}").Exists != true);

            foreach (TagLink tagLink in unusedTagLinks)
            {
                tagLink.Delete();
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private bool AddTags(IEnumerable<string> filePaths, ICollection<string> tagNames)
        {
            IEnumerable<Tag> requestedTags = tagNames
                .Select(GetOrCreateTag);

            var filesMappedToTags = filePaths
                .Select(filePath =>
                    new
                    {
                        FilePath = filePath,
                        TagsByFileName = Tag.GetByFile(filePath)
                    }
                )
                .ToList();

            IEnumerable<TagLink> missingTagLinks = filesMappedToTags
                .SelectMany(fileMappedToTags =>

                    requestedTags.Where(requestedTag =>

                            fileMappedToTags.TagsByFileName.All(existingTag =>
                                existingTag.TagId != requestedTag.TagId
                            )
                        )
                        .Select(missingTag => new TagLink
                        {
                            TagId = missingTag.TagId,
                            FilePath = fileMappedToTags.FilePath
                        })
                )
                .ToList();

            bool isSuccess = TagLink.CreateRange(missingTagLinks);

            return isSuccess;
        }

        private Tag GetOrCreateTag(string tagName)
        {
            Tag savedTag = Tag.GetByTagName(tagName);

            if (savedTag != null)
            {
                return savedTag;
            }

            // If the tag hasn't been saved yet, save it
            savedTag = new Tag
            {
                TagName = tagName
            };

            bool isSuccess = savedTag.Create();
            return isSuccess ? savedTag : null;
        }
    }
}