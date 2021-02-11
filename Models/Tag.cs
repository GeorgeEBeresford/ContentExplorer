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

        public static ICollection<Tag> GetByFileName(string fileName)
        {
            string query = @"SELECT Tags.TagId, Tags.TagName
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE Tags.FileName = @FileName";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FileName", fileName));
            }

            ICollection<Tag> tags = dataRows.Select(dataRow =>
                    new Tag(dataRow)
                )
                .ToList();

            return tags;
        }

        public bool Create()
        {
            const string query = @"INSERT INTO Tags (TagId, TagName) VALUES (@TagId, @TagName); SELECT last_insert_rowid()";

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