using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace ContentExplorer.Models
{
    public class Tag
    {
        public Tag()
        {
        }

        public Tag(IDictionary<string, object> rowValues) : this()
        {
            TagId = Convert.ToInt32(rowValues["TagId"]);
            TagName = Convert.ToString(rowValues["TagName"]);
        }

        public string TagName { get; set; }
        public int TagId { get; set; }

        public static ICollection<Tag> GetAll()
        {
            string query = @"SELECT Tags.TagId, Tags.TagName
                                FROM Tags";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dbContext.GetDataRows(query);

                dataRows = dbContext.GetDataRows(query);
            }

            ICollection<Tag> tags = dataRows.Select(dataRow =>
                    new Tag(dataRow)
                )
                .ToList();

            return tags;
        }

        public static ICollection<Tag> GetByDirectory(string directoryPath)
        {
            // The caller shouldn't have to care about which slash or case to use
            directoryPath = directoryPath.Replace("/", "\\").ToLowerInvariant();

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            directoryPath = directoryPath.TrimEnd("\\".ToCharArray());

            string query = @"SELECT Tags.TagId, Tags.TagName, TagLinks.FilePath, TagLinks.TagLinkId
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE LOWER(REPLACE(TagLinks.FilePath, '/', '\')) LIKE @FilePath
                                GROUP BY Tags.TagId, Tags.TagName";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FilePath", $"{directoryPath}\\%"));
            }

            ICollection<Tag> tags = dataRows.Select(dataRow =>
                    new Tag(dataRow)
                )
                .ToList();

            return tags;
        }

        public static Tag GetById (int tagId)
        {
            string query = @"SELECT TagId, TagName
                                FROM Tags
                                WHERE TagId = @TagId";

            IDictionary<string, object> dataRow;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRow = dbContext.GetDataRow(query, SqliteWrapper.GenerateParameter("@TagId", tagId));
            }

            return new Tag(dataRow);
        }

        public static ICollection<Tag> GetByFile(string filePath)
        {
            // The caller shouldn't have to care about which slash to use
            filePath = filePath.Replace("/", "\\");

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            filePath = filePath.ToLowerInvariant().TrimEnd("/\\".ToCharArray());

            string query = @"SELECT Tags.TagId, Tags.TagName
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE LOWER(REPLACE(TagLinks.FilePath, '/', '\')) = @FilePath
                                GROUP BY Tags.TagId, Tags.TagName";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FilePath", filePath));
            }

            ICollection<Tag> tags = dataRows.Select(dataRow =>
                    new Tag(dataRow)
                )
                .ToList();

            return tags;
        }

        public static Tag GetByTagName(string tagName)
        {
            string query = @"SELECT TagId, TagName
                                FROM Tags
                                WHERE TagName = @TagName";


            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@TagName", tagName));
            }

            Tag tag = dataRows.Select(dataRow =>
                    new Tag(dataRow)
                )
                .FirstOrDefault();

            return tag;
        }

        public static bool InitialiseTable()
        {
            string query = @"CREATE TABLE IF NOT EXISTS Tags (
                TagId INTEGER PRIMARY KEY,
                TagName TEXT NOT NULL
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
            const string query = @"INSERT INTO Tags (TagName) VALUES (@TagName); SELECT last_insert_rowid()";

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                TagId = Convert.ToInt32(dbContext.GetScalar(query, GenerateParameters()));
            }

            bool isSuccess = TagId != 0;

            return isSuccess;
        }

        public bool Delete()
        {
            const string query = @"DELETE FROM Tags WHERE TagId = @TagId";

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
                SqliteWrapper.GenerateParameter("@TagId", TagId),
                SqliteWrapper.GenerateParameter("@TagName", TagName)
            };
        }
    }
}