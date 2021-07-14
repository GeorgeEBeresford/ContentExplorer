using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Data.Sqlite;

namespace ContentExplorer.Models
{
    public class Tag
    {
        private static readonly List<string> CacheKeys = new List<string>();

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

        private static void ClearCaches()
        {
            foreach (string cacheKey in CacheKeys)
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }
        }

        public static ICollection<Tag> GetAll()
        {
            string cacheKey = "[Tags]";

            if (HttpContext.Current.Cache[cacheKey] is ICollection<Tag> cachedTags)
            {
                return cachedTags;
            }

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

            HttpContext.Current.Cache.Insert(cacheKey, tags, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tags;
        }

        public static ICollection<Tag> GetByFile(string filePath)
        {
            string cacheKey = $"[TagsByFile]{filePath}";

            if (HttpContext.Current.Cache[cacheKey] is ICollection<Tag> cachedTags)
            {
                return cachedTags;
            }

            // The caller shouldn't have to care about which slash to use
            filePath = filePath.Replace("/", "\\");

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            filePath = filePath.ToLowerInvariant().TrimEnd("/\\".ToCharArray());

            string query = @"SELECT Tags.TagId, Tags.TagName
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE TagLinks.FilePath = @FilePath
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

            HttpContext.Current.Cache.Insert(cacheKey, tags, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tags;
        }

        public static ICollection<Tag> GetByDirectory(string directoryPath, string[] filters, bool isRecursive = false)
        {
            string cacheKey = $"[TagsByDirectoryAndFilters]{directoryPath}{string.Join(",", filters)}";

            if (HttpContext.Current.Cache[cacheKey] is ICollection<Tag> cachedTags)
            {
                return cachedTags;
            }

            string[] stringParameters = new string[filters.Length];
            SqliteParameter[] dbParameters = new SqliteParameter[filters.Length + 2];

            for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
            {
                string stringParameter = $"@{filterIndex}";
                stringParameters[filterIndex] = stringParameter;
                dbParameters[filterIndex] = SqliteWrapper.GenerateParameter(stringParameter, filters[filterIndex]);
            }

            // Add the file path parameter to the end of the array
            dbParameters[filters.Length] = SqliteWrapper.GenerateParameter("@FilePath", $"{directoryPath}\\%");

            string query = $@"SELECT Tags.TagId, Tags.TagName, TagLinks.FilePath, TagLinks.TagLinkId
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE TagLinks.FilePath LIKE @FilePath
                                AND Tags.TagName IN ({string.Join(",", stringParameters)})
                                GROUP BY Tags.TagId, Tags.TagName";

            if (isRecursive != true)
            {
                query += " AND FilePath NOT LIKE @ExcludedFilePath";
                dbParameters[filters.Length + 1] = SqliteWrapper.GenerateParameter("@ExcludedFilePath", $"{directoryPath}\\%\\%");
            }

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, dbParameters);
            }

            ICollection<Tag> tags = dataRows
                .Select(dataRow =>
                    new Tag(dataRow)
                )
                .ToList();

            HttpContext.Current.Cache.Insert(cacheKey, tags, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tags;
        }

        public static Tag GetById(int tagId)
        {
            string cacheKey = $"[TagsById]{tagId}";

            if (HttpContext.Current.Cache[cacheKey] is Tag cachedTags)
            {
                return cachedTags;
            }

            string query = @"SELECT TagId, TagName
                                FROM Tags
                                WHERE TagId = @TagId";

            IDictionary<string, object> dataRow;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRow = dbContext.GetDataRow(query, SqliteWrapper.GenerateParameter("@TagId", tagId));
            }

            Tag tag = dataRow != null ? new Tag(dataRow) : null;

            if (tag != null)
            {
                HttpContext.Current.Cache.Insert(cacheKey, tag, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
                CacheKeys.Add(cacheKey);
            }

            return tag;
        }

        public static ICollection<Tag> GetByFile(string filePath, string[] filters)
        {
            string cacheKey = $"[TagsByFile]{filePath}";

            if (HttpContext.Current.Cache[cacheKey] is ICollection<Tag> cachedTags)
            {
                return cachedTags;
            }

            // The caller shouldn't have to care about which slash to use
            filePath = filePath.Replace("/", "\\");

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            filePath = filePath.ToLowerInvariant().TrimEnd("/\\".ToCharArray());

            string query = @"SELECT Tags.TagId, Tags.TagName
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE TagLinks.FilePath = @FilePath
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

            HttpContext.Current.Cache.Insert(cacheKey, tags, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tags;
        }

        public static Tag GetByTagName(string tagName)
        {
            string cacheKey = $"[TagByTagName]{tagName}";

            if (HttpContext.Current.Cache[cacheKey] is Tag cachedTags)
            {
                return cachedTags;
            }

            tagName = tagName.ToLowerInvariant();

            string query = @"SELECT TagId, TagName
                                FROM Tags
                                WHERE TagName = @TagName";


            IDictionary<string, object> dataRow;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRow = dbContext.GetDataRow(query, SqliteWrapper.GenerateParameter("@TagName", tagName));
            }

            Tag tag = dataRow != null ? new Tag(dataRow) : null;

            if (tag != null)
            {
                HttpContext.Current.Cache.Insert(cacheKey, tag, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
                CacheKeys.Add(cacheKey);
            }

            return tag;
        }

        public static bool InitialiseTable()
        {
            string query = @"CREATE TABLE IF NOT EXISTS Tags (
                TagId INTEGER PRIMARY KEY,
                TagName TEXT NOT NULL
            )";

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                try
                {
                    dbContext.ExecuteNonQuery(query);
                    return true;
                }
                catch (SqliteException)
                {
                    return false;
                }
            }
        }

        public bool Create()
        {
            const string query = @"INSERT INTO Tags (TagName) VALUES (@TagName); SELECT last_insert_rowid()";

            TagName = TagName.ToLowerInvariant();

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                TagId = Convert.ToInt32(dbContext.GetScalar(query, GenerateParameters()));
            }

            ClearCaches();

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

            ClearCaches();

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