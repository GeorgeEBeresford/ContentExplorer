using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace ContentExplorer.Models
{
    public class TagLink
    {
        public TagLink()
        {
        }

        public TagLink(IDictionary<string, object> rowValues) : this()
        {
            TagLinkId = Convert.ToInt32(rowValues["TagLinkId"]);
            TagId = Convert.ToInt32(rowValues["TagId"]);
            FilePath = Convert.ToString(rowValues["FilePath"]);
        }

        public int TagLinkId { get; set; }
        public int TagId { get; set; }
        public string FilePath { get; set; }
        private Tag Tag { get; set; }

        public Tag GetTag()
        {
            if (Tag != null)
            {
                return Tag;
            }

            Tag = Tag.GetById(TagId);
            return Tag;
        }

        public static ICollection<TagLink> GetByDirectory(string directoryPath)
        {
            // The caller shouldn't have to care about which slash or case to use
            directoryPath = directoryPath.Replace("/", "\\").ToLowerInvariant();

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            directoryPath = directoryPath.TrimEnd("\\".ToCharArray());

            string query = @"SELECT Tags.TagId, Tags.TagName, REPLACE(TagLinks.FilePath, '/', '\') AS [FilePath], TagLinks.TagLinkId
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE LOWER(REPLACE(TagLinks.FilePath, '/', '\')) LIKE @FilePath";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FilePath", $"{directoryPath}\\%"));
            }

            ICollection<TagLink> tagLinks = dataRows.Select(dataRow =>
                    new TagLink(dataRow)
                    {
                        Tag = new Tag(dataRow)
                    }
                )
                .ToList();

            return tagLinks;
        }

        public static ICollection<TagLink> GetByFile(string filePath)
        {
            // The caller shouldn't have to care about which slash or case to use
            filePath = filePath.Replace("/", "\\").ToLowerInvariant();

            string query = @"SELECT REPLACE(TagLinks.FilePath, '/', '\') AS [FilePath], TagId, TagLinkId
                                FROM TagLinks
                                WHERE LOWER(REPLACE(TagLinks.FilePath, '/', '\')) = @FilePath";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FilePath", filePath));
            }

            ICollection<TagLink> tagLinks = dataRows.Select(dataRow =>

                    new TagLink(dataRow)
                )
                .ToList();

            return tagLinks;
        }

        public static ICollection<TagLink> GetByTagName(string tagName)
        {
            string query = @"SELECT REPLACE(TagLinks.FilePath, '/', '\') AS [FilePath], TagLinks.TagId, TagLinks.TagLinkId
                                FROM TagLinks
                                INNER JOIN Tags ON TagLinks.TagId = Tags.TagId
                                WHERE Tags.TagName = @TagName";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@TagName", tagName));
            }

            ICollection<TagLink> tagLinks = dataRows.Select(dataRow =>

                    new TagLink(dataRow)
                )
                .ToList();

            return tagLinks;
        }

        public static bool InitialiseTable()
        {
            string query = @"CREATE TABLE IF NOT EXISTS TagLinks (
                TagLinkId INTEGER PRIMARY KEY,
                TagId INT,
                FilePath TEXT NOT NULL,
                FOREIGN KEY(TagId) REFERENCES Tags(TagId)
            )";

            bool isSuccess;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                isSuccess = dbContext.ExecuteNonQuery(query);
            }

            return isSuccess;
        }

        public bool Create()
        {
            // We may need to search for paths later and we don't want to be dealing with differences in slashes
            FilePath.Replace("/", "\\");

            const string query = @"INSERT INTO TagLinks (TagId, FilePath) VALUES (@TagId, @FilePath); SELECT last_insert_rowid()";

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                TagLinkId = Convert.ToInt32(dbContext.GetScalar(query, GenerateParameters()));
            }

            bool isSuccess = TagLinkId != 0;

            return isSuccess;
        }

        public bool Delete()
        {
            const string query = @"DELETE FROM TagLinks WHERE TagLinkId = @TagLinkId";

            bool isSuccess;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                isSuccess = dbContext.ExecuteNonQuery(query, GenerateParameters());
            }

            return isSuccess;
        }

        private ICollection<SqliteParameter> GenerateParameters()
        {
            return new List<SqliteParameter>
            {
                SqliteWrapper.GenerateParameter("@TagLinkId", TagLinkId),
                SqliteWrapper.GenerateParameter("@TagId", TagId),
                SqliteWrapper.GenerateParameter("@FilePath", FilePath)
            };
        }
    }
}