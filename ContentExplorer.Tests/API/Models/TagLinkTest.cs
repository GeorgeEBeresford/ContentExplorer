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
            CreateAndReturnTagLink();
            ICollection<TagLink> directoryTagLinks = TagLink.GetByDirectory(TestImagesDirectory);
            Assert.IsNotNull(directoryTagLinks, "Null was returned for directory tag links");
            Assert.AreNotEqual(0, directoryTagLinks.Count, "No tag links were returned for directory");
        }

        [TestMethod]
        public void GetAllTagLinks()
        {
            CreateAndReturnTagLink();
            CreateAndReturnTagLink();

            ICollection<TagLink> allTagLinks = TagLink.GetAll();
            Assert.IsNotNull(allTagLinks, "Null was returned for directory tag links");
            Assert.AreEqual(2, allTagLinks.Count);
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

            // Add a new file with just the default tag so we can make sure multiple filters return the correct files
            Tag singleMatchTag = CreateAndReturnTag();
            TagLink singleMatchTagLink = new TagLink
            {
                TagId = singleMatchTag.TagId,
                FilePath = $"{TestImagesDirectory}\\test3.png"
            };
            singleMatchTagLink.Create();

            // Check for default tag
            ICollection<TagLink> nonRecursiveTagLinks =
                TagLink.GetByDirectory(TestImagesDirectory, new[] {DefaultTestTagName});

            Assert.IsNotNull(nonRecursiveTagLinks, "Returned tag links were null");
            Assert.AreEqual(2, nonRecursiveTagLinks.Count);

            ICollection<TagLink> recursiveTagLinks =
                TagLink.GetByDirectory(TestImagesDirectory, new[] {DefaultTestTagName}, true);

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

        [TestMethod]
        public void GetTagLinkByTagName()
        {
            CreateAndReturnTagLink();
            CreateAndReturnTagLink();

            ICollection<TagLink> tagLinks = TagLink.GetByTagName(DefaultTestTagName);
            Assert.IsNotNull(tagLinks, "Returned tag links are null");
            Assert.AreEqual(2, tagLinks.Count);
        }

        [TestMethod]
        public void CreateTagLink()
        {
            TagLink tagLink = CreateAndReturnTagLink();

            using (SqliteWrapper sqliteWrapper = new SqliteWrapper("AppDb"))
            {
                IDictionary<string, object> dataRow = sqliteWrapper.GetDataRow(
                    "SELECT * FROM TagLinks WHERE TagLinkId = @TagLinkId",
                    SqliteWrapper.GenerateParameter("@TagLinkId", tagLink.TagLinkId)
                );

                Assert.IsNotNull(dataRow);
            }
        }

        [TestMethod]
        public void DeleteTagLink()
        {
            TagLink tagLink = CreateAndReturnTagLink();
            bool isSuccess = tagLink.Delete();
            Assert.IsTrue(isSuccess, "TagLink was not successfully deleted");

            using (SqliteWrapper sqliteWrapper = new SqliteWrapper("AppDb"))
            {
                IDictionary<string, object> dataRow = sqliteWrapper.GetDataRow(
                    "SELECT * FROM TagLinks WHERE TagLinkId = @TagLinkId",
                    SqliteWrapper.GenerateParameter("@TagLinkId", tagLink.TagLinkId)
                );

                Assert.IsNull(dataRow);
            }
        }
    }
}