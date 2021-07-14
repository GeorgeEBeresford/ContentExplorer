using System.Collections.Generic;
using ContentExplorer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContentExplorer.Tests.API.Models
{
    [TestClass]
    public class TagLinkTest : Test
    {
        [TestMethod]
        public void GetTag()
        {
            Tag tag = CreateAndReturnTag();
            TagLink createdTagLink = new TagLink
            {
                TagId = tag.TagId
            };

            Tag retrievedTag = createdTagLink.GetTag();
            Assert.IsNotNull(retrievedTag, "Tag was not retrieved");
            Assert.AreEqual(tag.TagId, retrievedTag.TagId);

            TagLink tagLink = CreateAndReturnTagLink();
            retrievedTag = tagLink.GetTag();
            Assert.IsNotNull(tag, "Tag was not retrirved");
            Assert.AreEqual(tagLink.TagId, retrievedTag.TagId);
        }

        [TestMethod]
        public void GetTagLinkByDirectory()
        {
            TagLink tagLink = CreateAndReturnTagLink();
            ICollection<TagLink> directoryTagLinks = TagLink.GetByDirectory(TestImagesDirectory);
            Assert.IsNotNull(directoryTagLinks, "Null was returned for directory tag links");
            Assert.AreNotEqual(0, directoryTagLinks.Count, "No tag links were returned for directory");
        }

        [TestMethod]
        public void GetTagLinkByFilteredDirectory()
        {
            // Default tag
            CreateAndReturnTagLink();
            CreateAndReturnTagLink("Nested");

            // Second tag
            CreateAndReturnTagLink(null, "Filtered");
            CreateAndReturnTagLink("Nested", "Filtered");

            // Non matching tags (to make sure it's actually filtered)
            Tag nonMatchingTag = CreateAndReturnTag("NonMatchingFilter");
            TagLink nonMatchingTagLink = new TagLink
            {
                TagId = nonMatchingTag.TagId,
                FilePath = $"{TestImagesDirectory}\\test2.png"
            };
            nonMatchingTagLink.Create();

            nonMatchingTagLink.FilePath = $"{TestImagesDirectory}\\Nested\\test2.png";
            nonMatchingTagLink.Create();

            Tag singleMatchTag = CreateAndReturnTag();
            TagLink singleMatchTagLink = new TagLink
            {
                TagId = singleMatchTag.TagId,
                FilePath = $"{TestImagesDirectory}\\test3.png"
            };
            singleMatchTagLink.Create();

            // Check for default tag
            ICollection<TagLink> nonRecursiveTagLinks =
                TagLink.GetByDirectory(TestImagesDirectory, new[] { DefaultTestTagName });

            Assert.IsNotNull(nonRecursiveTagLinks, "Returned tag links were null");
            Assert.AreEqual(2, nonRecursiveTagLinks.Count);

            ICollection<TagLink> recursiveTagLinks =
                TagLink.GetByDirectory(TestImagesDirectory, new[] { DefaultTestTagName }, true);

            Assert.IsNotNull(recursiveTagLinks, "Returned tag links were null");
            Assert.AreEqual(3, recursiveTagLinks.Count);

            // Check for two tags
            nonRecursiveTagLinks =
                TagLink.GetByDirectory(TestImagesDirectory, new[] {DefaultTestTagName, "Filtered"});

            Assert.IsNotNull(nonRecursiveTagLinks, "Returned tag links were null");
            Assert.AreEqual(1, nonRecursiveTagLinks.Count);

            recursiveTagLinks =
                TagLink.GetByDirectory(TestImagesDirectory, new[] {DefaultTestTagName, "Filtered"}, true);

            Assert.IsNotNull(recursiveTagLinks, "Returned tag links were null");
            Assert.AreEqual(2, recursiveTagLinks.Count);
        }
    }
}