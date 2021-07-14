using System.Configuration;
using System.IO;
using ContentExplorer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContentExplorer.Tests.API
{
    [TestClass]
    public abstract class Test
    {
        protected Test()
        {
            FakeHttpContext = new FakeHttpContext.FakeHttpContext();
        }

        private FakeHttpContext.FakeHttpContext FakeHttpContext { get; }

        protected string TestTagName => "TestTag";

        [TestInitialize]
        public void Initialise()
        {
            bool tagsAreInitialised = Tag.InitialiseTable();
            Assert.IsTrue(tagsAreInitialised);

            string sql = "SELECT * FROM Tags LIMIT 1";
            using (SqliteWrapper sqliteWrapper = new SqliteWrapper("AppDb"))
            {
                sqliteWrapper.GetDataRow(sql);
            }

            bool tagLinksAreInitialised = TagLink.InitialiseTable();
            Assert.IsTrue(tagLinksAreInitialised);

            sql = "SELECT * FROM TagLinks LIMIT 1";
            using (SqliteWrapper sqliteWrapper = new SqliteWrapper("AppDb"))
            {
                sqliteWrapper.GetDataRow(sql);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            FileInfo fileInfo = new FileInfo("database.db");
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            string cdnDiskPath = ConfigurationManager.AppSettings["BaseDirectory"];
            string imagePath = $"{cdnDiskPath}\\{ConfigurationManager.AppSettings["ImagesPath"]}";
            string testFilePath = $"{imagePath}\\test.png";
            fileInfo = new FileInfo(testFilePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            FakeHttpContext.Dispose();
        }

        protected FileInfo CreateAndReturnImage()
        {
            string cdnDiskPath = ConfigurationManager.AppSettings["BaseDirectory"];

            DirectoryInfo directoryInfo = new DirectoryInfo(cdnDiskPath);
            if (directoryInfo.Exists != true)
            {
                directoryInfo.Create();
            }

            string imagePath = $"{cdnDiskPath}\\{ConfigurationManager.AppSettings["ImagesPath"]}";
            directoryInfo = new DirectoryInfo(imagePath);
            if (directoryInfo.Exists != true)
            {
                directoryInfo.Create();
            }


            // The image doesn't have to be readable. We just have to be able to guess it's an image from the extension
            string testFilePath = $"{imagePath}\\test.png";
            using (StreamWriter streamWriter = new StreamWriter(new FileStream(testFilePath, FileMode.CreateNew)))
            {
                streamWriter.WriteLine("Test file");
            }

            FileInfo testFileInfo = new FileInfo(testFilePath);
            return testFileInfo;
        }

        protected TagLink CreateAndReturnTagLink()
        {
            Tag tag = CreateAndReturnTag();
            FileInfo fileInfo = CreateAndReturnImage();

            TagLink tagLink = new TagLink
            {
                TagId = tag.TagId,
                FilePath = "Images/test.png"
            };

            bool isSuccess = tagLink.Create();
            Assert.IsTrue(isSuccess, "TagLink was not successfully created");
            Assert.AreNotEqual(0, tag.TagId, "TagLink ID was not set");

            return tagLink;
        }

        protected Tag CreateAndReturnTag()
        {
            Tag tag = new Tag
            {
                TagName = TestTagName
            };

            bool isSuccess = tag.Create();
            Assert.IsTrue(isSuccess, "Tag was not successfully created");
            Assert.AreNotEqual(0, tag.TagId, "Tag ID was not set");

            return tag;
        }
    }
}