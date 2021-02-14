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
        [HttpPost]
        public JsonResult SetTags(string filePath, string tags)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists == false)
            {
                return Json(false);
            }

            ICollection<Tag> tagsByFileName = Tag.GetByFileName(filePath);
            ICollection<string> splitTags = tags.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (string splitTag in splitTags)
            {
                Tag savedTag = Tag.GetByTagName(splitTag);

                if (savedTag == null)
                {
                    // If the tag hasn't been saved yet, save it
                    savedTag = new Tag
                    {
                        TagName = splitTag
                    };

                    bool isSuccess = savedTag.Create();

                    if (!isSuccess)
                    {
                        return Json(false);
                    }

                    // Add it to the collection so we can reference it later
                    tagsByFileName.Add(savedTag);
                }

                bool tagLinkExists = tagsByFileName.Any(tagByFileName => tagByFileName.TagName == splitTag);

                if (!tagLinkExists)
                {
                    // If the tag link isn't saved yet, save it
                    TagLink tagLink = new TagLink
                    {
                        FileName = filePath,
                        TagId = savedTag.TagId
                    };

                    bool isSuccess = tagLink.Create();

                    if (!isSuccess)
                    {
                        return Json(false);
                    }
                }
            }

            ICollection<TagLink> tagLinks = TagLink.GetByFileName(filePath);

            foreach (Tag tag in tagsByFileName)
            {
                bool isTagRequested = splitTags.Any(splitTag => tag.TagName == splitTag);

                if (!isTagRequested)
                {
                    TagLink matchingTagLink = tagLinks.FirstOrDefault(tagLink => tagLink.TagId == tag.TagId);

                    // If the tag has not been requested, delete it from our saved links
                    if (matchingTagLink != null)
                    {
                        bool isSuccess = matchingTagLink.Delete();
                        return Json(false);
                    }
                }
            }

            return Json(true);
        }
    }
}