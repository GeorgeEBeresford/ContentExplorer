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
            FileName = Convert.ToString(rowValues["FileName"]);
        }

        public int TagLinkId { get; set; }
        public int TagId { get; set; }
        public string FileName { get; set; }

        public static ICollection<TagLink> GetByFileName(string fileName)
        {
            string query = @"SELECT FileName, TagId, TagLinkId
                                FROM TagLinks
                                WHERE FileName = @FileName";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FileName", fileName));
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
                TagLinkId INT,
                TagId INT,
                FileName VARCHAR(257)
            )";

            bool isSuccess;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                isSuccess = dbContext.ExecuteNonQuery(query);
            }

            return isSuccess;
        }

        public static ICollection<TagLink> GetByTagName(string tagName)
        {
            string query = @"SELECT FileName, TagId, TagLinkId
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

        public bool Create()
        {
            const string query = @"INSERT INTO TagLinks (TagLinkId, TagId, FileName) VALUES (@TagLinkId, @TagId, @FileName); SELECT last_insert_rowid()";

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
                SqliteWrapper.GenerateParameter("@FileName", FileName)
            };
        }
    }
}