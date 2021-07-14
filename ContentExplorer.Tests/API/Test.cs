using System.Configuration;
using System.IO;
using ContentExplorer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContentExplorer.Tests.API
{
    public abstract class Test
    {
        protected Test()
        {
            FakeHttpContext = new FakeHttpContext.FakeHttpContext();
        }

        private FakeHttpContext.FakeHttpContext FakeHttpContext { get; }

        protected const string DefaultTestTagName = "TestTag";
        protected const string DefaultTestImageName = "test.png";

        protected static readonly string TestImagesDirectory = ConfigurationManager.AppSettings["ImagesPath"];
        protected static readonly string DefaultTestImagePath = $"{TestImagesDirectory}\\{DefaultTestImageName}";


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

            fileInfo = new FileInfo(DefaultTestImagePath);
            if (fileInfo.Exists)
            {
                try
                {
                    fileInfo.Delete();
                }
                // File may be in-use. Try again.
                catch (IOException)
                {
                    fileInfo.Delete();
                }
            }

            FakeHttpContext.Dispose();
        }

        protected FileInfo CreateAndReturnImage(string relativeDirectory = null)
        {
            string cdnDiskPath = ConfigurationManager.AppSettings["BaseDirectory"];
            string directoryPath = relativeDirectory == null
                ? $"{cdnDiskPath}\\{TestImagesDirectory}"
                : $"{cdnDiskPath}\\{TestImagesDirectory}\\{relativeDirectory}";

            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            if (directoryInfo.Exists != true)
            {
                directoryInfo.Create();
            }

            // We don't really need multiple files in a directory. Just attach multiple tag links to the same file.

            string filePath = $"{cdnDiskPath}\\{DefaultTestImagePath}";
            FileInfo createdFile = new FileInfo(filePath);
            if (createdFile.Exists)
            {
                createdFile.Delete();
            }

            // The image doesn't have to be readable. We just have to be able to guess it's an image from the extension
            using (StreamWriter streamWriter = new StreamWriter(new FileStream(filePath, FileMode.CreateNew)))
            {
                streamWriter.WriteLine("Test file");
            }

            FileInfo testFileInfo = new FileInfo(filePath);
            return testFileInfo;
        }

        protected TagLink CreateAndReturnTagLink(string relativeDirectory = null, string tagName = null)
        {
            Tag tag = CreateAndReturnTag(tagName);

            CreateAndReturnImage(relativeDirectory);

            string filePath = relativeDirectory == null
                ? DefaultTestImagePath
                : $"{TestImagesDirectory}\\{relativeDirectory}\\{DefaultTestImageName}";

            TagLink tagLink = new TagLink
            {
                TagId = tag.TagId,
                FilePath = filePath
            };

            bool isSuccess = tagLink.Create();
            Assert.IsTrue(isSuccess, "TagLink was not successfully created");
            Assert.AreNotEqual(0, tag.TagId, "TagLink ID was not set");

            return tagLink;
        }

        protected Tag CreateAndReturnTag(string tagName = null)
        {
            Tag tag = new Tag
            {
                TagName = tagName ?? DefaultTestTagName
            };

            bool isSuccess = tag.Create();
            Assert.IsTrue(isSuccess, "Tag was not successfully created");
            Assert.AreNotEqual(0, tag.TagId, "Tag ID was not set");

            return tag;
        }
    }
}