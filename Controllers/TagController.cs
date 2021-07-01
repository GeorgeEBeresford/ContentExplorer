using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ContentExplorer.Models;

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

            bool isSuccess = true;
            foreach (string diskLocation in diskLocations)
            {
                isSuccess &= AddTags(diskLocation, tags);
            }

            return Json(isSuccess);
        }

        [HttpPost]
        public JsonResult AddTagsToDirectories(string[] directoryPaths, string[] tags, string mediaType)
        {
            // Filter inputs that would make no sense
            directoryPaths = directoryPaths.Where(filePath => filePath != "").ToArray();
            tags = tags.Where(tag => tag != "").Select(tag => tag.Trim()).ToArray();

            string rootDirectory = GetRootDirectory(mediaType);
            directoryPaths = directoryPaths.Select(directoryPath => rootDirectory + "\\" + directoryPath).ToArray();

            bool isSuccess = true;
            foreach (string directoryPath in directoryPaths)
            {
                DirectoryInfo directory = new DirectoryInfo(directoryPath);
                if (directory.Exists)
                {
                    FileInfo[] subFiles = directory.GetFiles("*.*", SearchOption.AllDirectories);
                    foreach (FileInfo subFile in subFiles)
                    {
                        isSuccess &= AddTags(subFile.FullName, tags);
                    }
                }
            }

            return Json(isSuccess);
        }

        [HttpPost]
        public JsonResult SetTags(string[] filePaths, string[] tags)
        {
            // Filter inputs that would make no sense
            filePaths = filePaths.Where(filePath => filePath != "").ToArray();
            tags = tags.Where(tag => tag != "").ToArray();

            bool isSuccess = true;
            foreach (string filePath in filePaths)
            {
                isSuccess &= AddTags(filePath, tags);
                isSuccess &= RemoveUnusedTagLinks(filePath, tags);
            }

            return Json(isSuccess);
        }

        [HttpGet]
        public JsonResult GetDirectoryTags(string directoryName, string mediaType)
        {
            directoryName = GetRootDirectory(mediaType) + "\\" + directoryName;

            ICollection<Tag> tags = Tag.GetByDirectory(directoryName)
                .OrderBy(tag => tag.TagName)
                .ToArray();

            return Json(tags, JsonRequestBehavior.AllowGet);
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

        private bool AddTags(string filePath, IEnumerable<string> tagNames)
        {
            string webDiskLocation = ConfigurationManager.AppSettings["BaseDirectory"];
            FileInfo fileInfo = new FileInfo(Path.Combine(webDiskLocation, filePath));

            if (fileInfo.Exists == false)
            {
                return false;
            }

            ICollection<Tag> tagsByFileName = Tag.GetByFile(filePath);

            bool isSuccess = true;
            foreach (string tagName in tagNames)
            {
                Tag savedTag = GetOrCreateTag(tagName);

                if (savedTag == null)
                {
                    isSuccess = false;
                }

                if (isSuccess)
                {
                    bool tagLinkExists = tagsByFileName.Any(tagByFileName => tagByFileName.TagName == tagName);

                    if (tagLinkExists == false)
                    {
                        TagLink savedTagLink = LinkFileToTag(filePath, savedTag.TagId);

                        if (savedTagLink == null)
                        {
                            isSuccess = false;
                        }
                        else
                        {
                            tagsByFileName.Add(savedTag);
                        }
                    }
                }
            }

            return isSuccess;
        }

        private TagLink LinkFileToTag(string filePath, int tagId)
        {
            // If the tag link isn't saved yet, save it
            TagLink tagLink = new TagLink
            {
                FilePath = filePath,
                TagId = tagId
            };

            bool isSuccess = tagLink.Create();

            return isSuccess ? tagLink : null;
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

        private bool RemoveUnusedTagLinks(string filePath, ICollection<string> requestedTags)
        {
            ICollection<Tag> tagsByFileName = Tag.GetByFile(filePath);
            ICollection<TagLink> tagLinks = TagLink.GetByFile(filePath);

            bool isSuccess = true;
            foreach (Tag tag in tagsByFileName)
            {
                bool isTagRequested = requestedTags.Any(splitTag => tag.TagName == splitTag);

                if (!isTagRequested)
                {
                    TagLink matchingTagLink = tagLinks.FirstOrDefault(tagLink => tagLink.TagId == tag.TagId);

                    // If the tag has not been requested, delete it from our saved links
                    if (matchingTagLink != null)
                    {
                        isSuccess = matchingTagLink.Delete();
                    }
                }
            }

            return isSuccess;
        }
    }
}