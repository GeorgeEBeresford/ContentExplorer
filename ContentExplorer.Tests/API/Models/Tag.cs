using System.Collections.Generic;
using ContentExplorer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContentExplorer.Tests.API.Models
{
    [TestClass]
    public class TagTest : Test
    {
        [TestMethod]
        public void CreateTag()
        {
            Tag tag = CreateAndReturnTag();

            using (SqliteWrapper sqliteWrapper = new SqliteWrapper("AppDb"))
            {
                IDictionary<string, object> dataRow = sqliteWrapper.GetDataRow(
                    "SELECT * FROM Tags WHERE TagId = @TagId",
                    SqliteWrapper.GenerateParameter("@TagId", tag.TagId)
                );

                Assert.IsNotNull(dataRow);
            }
        }

        [TestMethod]
        public void DeleteTag()
        {
            Tag tag = CreateAndReturnTag();
            bool isSuccess = tag.Delete();
            Assert.IsTrue(isSuccess, "Tag was not successfully deleted");

            using (SqliteWrapper sqliteWrapper = new SqliteWrapper("AppDb"))
            {
                IDictionary<string, object> dataRow = sqliteWrapper.GetDataRow(
                    "SELECT * FROM Tags WHERE TagId = @TagId",
                    SqliteWrapper.GenerateParameter("@TagId", tag.TagId)
                );

                Assert.IsNull(dataRow);
            }
        }

        [TestMethod]
        public void GetTagByName()
        {
            Tag tag = CreateAndReturnTag();
            Tag foundTag = Tag.GetByTagName(tag.TagName);

            Assert.IsNotNull(foundTag);
            Assert.AreEqual(tag.TagId, foundTag.TagId);
            Assert.AreEqual(tag.TagName, foundTag.TagName);
        }

        [TestMethod]
        public void GetByFile()
        {
            TagLink tagLink = CreateAndReturnTagLink();
            ICollection<Tag> tags = Tag.GetByFile(tagLink.FilePath);

            Assert.IsNotNull(tags, "Returned tag collection was null");
            Assert.AreNotEqual(0, tags.Count, "No matching tags were found");
        }

        [TestMethod]
        public void GetByFileAndFilters()
        {
            TagLink tagLink = CreateAndReturnTagLink();
            ICollection<Tag> tags = Tag.GetByFile(tagLink.FilePath, new []{ TestTagName });

            Assert.IsNotNull(tags, "Returned tag collection was null");
            Assert.AreNotEqual(0, tags.Count, "No matching tags were found");
        }

        [TestMethod]
        public void GetAll()
        {
            Tag tag = CreateAndReturnTag();
            ICollection<Tag> tags = Tag.GetAll();

            Assert.IsNotNull(tags, "Returned tag collection was null");
            Assert.AreNotEqual(0, tags.Count, "No matching tags were found");
        }
    }
}