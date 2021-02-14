using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ContentExplorer.Models;

namespace ContentExplorer.Controllers
{
    public class TagController : Controller
    {
        [Route(Name = "AddTags")]
        [HttpPost]
        public JsonResult AddTagsJson(string filePath, string tags)
        {
            ICollection<string> tagNames = tags.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            bool isSuccess = AddTags(filePath, tagNames);

            return Json(isSuccess);
        }

        [HttpPost]
        public JsonResult SetTags(string filePath, string tags)
        {
            ICollection<string> tagNames = tags.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            AddTags(filePath, tagNames);
            RemoveUnusedTagLinks(filePath, tagNames);

            return Json(true);
        }

        private bool AddTags(string filePath, IEnumerable<string> tagNames)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists == false)
            {
                return false;
            }

            ICollection<Tag> tagsByFileName = Tag.GetByFileName(filePath);

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
                    // Add it to the collection so we can reference it later
                    tagsByFileName.Add(savedTag);
                    bool tagLinkExists = tagsByFileName.Any(tagByFileName => tagByFileName.TagName == tagName);

                    if (tagLinkExists == false)
                    {
                        TagLink savedTagLink = LinkFileToTag(filePath, savedTag.TagId);

                        if (savedTagLink == null)
                        {
                            isSuccess = false;
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
                FileName = filePath,
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
            ICollection<Tag> tagsByFileName = Tag.GetByFileName(filePath);
            ICollection<TagLink> tagLinks = TagLink.GetByFileName(filePath);

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