using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Web;
using Microsoft.Data.Sqlite;

namespace ContentExplorer.Models
{
    public class TagLink
    {
        private static readonly List<string> CacheKeys = new List<string>();

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

        public static bool UpdateDirectoryPath(string oldDirectoryPath, string newDirectoryPath)
        {
            // The caller shouldn't have to care about which slash or case to use
            oldDirectoryPath = oldDirectoryPath.Replace("/", "\\").ToLowerInvariant();
            newDirectoryPath = newDirectoryPath.Replace("/", "\\").ToLowerInvariant();

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            oldDirectoryPath = oldDirectoryPath.TrimEnd("\\".ToCharArray());
            newDirectoryPath = newDirectoryPath.TrimEnd("\\".ToCharArray());

            // Replaces any instances of the old directory path it finds with the new one. This may change rows unexpectedly. E.g. pictures/test/pictures/test/2.jpg
            // Todo - Find a better way to do this
            string query = @"UPDATE TagLinks
                            SET FilePath = REPLACE(FilePath, @OldDirectoryPath, @NewDirectoryPath)
                            WHERE FilePath LIKE @OldDirectoryPathLike
                            AND FilePath NOT LIKE @OldDirectoryPathRecursionLike";

            ICollection<SqliteParameter> dbParameters = new List<SqliteParameter>
            {
                SqliteWrapper.GenerateParameter("@NewDirectoryPath", $"{newDirectoryPath}\\"),
                SqliteWrapper.GenerateParameter("@OldDirectoryPath", $"{oldDirectoryPath}\\"),
                SqliteWrapper.GenerateParameter("@OldDirectoryPathLike", $"{oldDirectoryPath}\\%"),
                SqliteWrapper.GenerateParameter("@OldDirectoryPathRecursionLike", $"{oldDirectoryPath}\\%\\%")
            };

            bool isSuccess;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                isSuccess = dbContext.ExecuteNonQuery(query, dbParameters);
            }

            return isSuccess;
        }

        public static bool UpdateFilePath(string oldFilePath, string newFilePath)
        {
            // The caller shouldn't have to care about which slash or case to use
            oldFilePath = oldFilePath.Replace("/", "\\").ToLowerInvariant();
            newFilePath = newFilePath.Replace("/", "\\").ToLowerInvariant();

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            oldFilePath = oldFilePath.TrimEnd("\\".ToCharArray());
            newFilePath = newFilePath.TrimEnd("\\".ToCharArray());

            string query = @"UPDATE TagLinks SET FilePath = @NewFilePath WHERE FilePath = @OldFilePath";
            ICollection<SqliteParameter> dbParameters = new List<SqliteParameter>
            {
                SqliteWrapper.GenerateParameter("@NewFilePath", newFilePath),
                SqliteWrapper.GenerateParameter("@OldFilePath", oldFilePath)
            };

            bool isSuccess;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                isSuccess = dbContext.ExecuteNonQuery(query, dbParameters);
            }

            return isSuccess;
        }

        public static ICollection<TagLink> GetByFileName(string filePath)
        {
            string cacheKey = $"[TagLinksByFile]{filePath}";
            if (HttpContext.Current.Cache[cacheKey] is ICollection<TagLink> cachedTags)
            {
                return cachedTags;
            }

            // The caller shouldn't have to care about which slash or case to use
            filePath = filePath.Replace("/", "\\").ToLowerInvariant();

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            filePath = filePath.TrimEnd("\\".ToCharArray());

            string query = @"SELECT Tags.TagId, Tags.TagName, TagLinks.FilePath AS [FilePath], TagLinks.TagLinkId
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE TagLinks.FilePath = @FullName";


            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, SqliteWrapper.GenerateParameter("@FullName", filePath));
            }

            ICollection<TagLink> tagLinks = dataRows
                .Select(dataRow =>
                    new TagLink(dataRow)
                )
                .ToList();

            HttpContext.Current.Cache.Insert(cacheKey, tagLinks, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tagLinks;
        }

        public static ICollection<TagLink> GetAll()
        {
            string cacheKey = "[TagLinksAll]";
            if (HttpContext.Current.Cache[cacheKey] is ICollection<TagLink> cachedTags)
            {
                return cachedTags;
            }

            string query = @"SELECT TagLinks.FilePath, TagLinks.TagLinkId, TagLinks.TagId
                           FROM TagLinks";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query);
            }

            ICollection<TagLink> tagLinks = dataRows
                .Select(dataRow =>
                    new TagLink(dataRow)
                )
                .ToList();

            HttpContext.Current.Cache.Insert(cacheKey, tagLinks, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tagLinks;
        }

        public static ICollection<TagLink> GetByDirectory(string directoryPath)
        {
            string cacheKey = $"[TagLinksByDirectory]{directoryPath}";
            if (HttpContext.Current.Cache[cacheKey] is ICollection<TagLink> cachedTags)
            {
                return cachedTags;
            }

            // The caller shouldn't have to care about which slash or case to use
            directoryPath = directoryPath.Replace("/", "\\").ToLowerInvariant();

            // We need to distinguish between files and directories with the same name but the caller shouldn't have to worry about it
            directoryPath = directoryPath.TrimEnd("\\".ToCharArray());

            string query =
                @"SELECT Tags.TagId, Tags.TagName, TagLinks.FilePath AS [FilePath], TagLinks.TagLinkId
                                FROM Tags
                                INNER JOIN TagLinks ON Tags.TagId = TagLinks.TagId
                                WHERE TagLinks.FilePath LIKE @FilePath";

            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query,
                    SqliteWrapper.GenerateParameter("@FilePath", $"{directoryPath}\\%"));
            }

            ICollection<TagLink> tagLinks = dataRows.Select(dataRow =>
                    new TagLink(dataRow)
                    {
                        Tag = new Tag(dataRow)
                    }
                )
                .ToList();

            HttpContext.Current.Cache.Insert(cacheKey, tagLinks, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tagLinks;
        }

        public static ICollection<TagLink> GetByDirectory(string directoryPath, string[] filters, bool isRecursive = false)
        {
            filters = filters.Select(filter => filter.ToLowerInvariant()).ToArray();

            string cacheKey = $"[TagLinksByDirectoryAndFilters]{directoryPath}{string.Join(",", filters)}{isRecursive}";
            if (HttpContext.Current.Cache[cacheKey] is ICollection<TagLink> cachedTags)
            {
                return cachedTags;
            }

            string query = $@"WITH FilePathTags AS (
	                            SELECT
		                            (',' || GROUP_CONCAT(Tags.TagName) || ',') AS [CombinedTags],
		                            TagLinks.FilePath
	                            FROM TagLinks
	                            INNER JOIN Tags ON Tags.TagId = TagLinks.TagId
	                            GROUP BY TagLinks.FilePath
                            )
                            SELECT
	                            TagLinks.FilePath,
	                            TagLinks.TagLinkId,
	                            Tags.TagId,
	                            Tags.TagName
                            FROM TagLinks
                            INNER JOIN Tags ON Tags.TagId = TagLinks.TagId
                            INNER JOIN FilePathTags ON FilePathTags.FilePath = TagLinks.FilePath
                            WHERE TagLinks.FilePath LIKE @FilePath";

            SqliteParameter[] dbParameters = new SqliteParameter[filters.Length + 2];

            for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
            {
                string stringParameter = $"@{filterIndex}";
                query += $" AND FilePathTags.CombinedTags LIKE {stringParameter}";
                dbParameters[filterIndex] = SqliteWrapper.GenerateParameter(stringParameter, $"%,{filters[filterIndex]},%");
            }

            // Add the file path parameter to the end of the array
            dbParameters[filters.Length] = SqliteWrapper.GenerateParameter("@FilePath", $"{directoryPath}\\%");

            if (isRecursive != true)
            {
                query += " AND TagLinks.FilePath NOT LIKE @ExcludedFilePath";
                dbParameters[filters.Length + 1] = SqliteWrapper.GenerateParameter("@ExcludedFilePath", $"{directoryPath}\\%\\%");
            }

            query += " GROUP BY TagLinks.FilePath";


            ICollection<IDictionary<string, object>> dataRows;
            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dataRows = dbContext.GetDataRows(query, dbParameters);
            }

            ICollection<TagLink> tagLinks = dataRows
                .Select(dataRow =>
                    new TagLink(dataRow)
                    {
                        Tag = new Tag(dataRow)
                    }
                )
                .ToList();

            HttpContext.Current.Cache.Insert(cacheKey, tagLinks, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

            return tagLinks;
        }

        public static ICollection<TagLink> GetByTagName(string tagName)
        {
            tagName = tagName.ToLowerInvariant();

            string cacheKey = $"[TagLinksByTagName]{tagName}";
            if (HttpContext.Current.Cache[cacheKey] is ICollection<TagLink> cachedTags)
            {
                return cachedTags;
            }

            string query =
                @"SELECT TagLinks.TagLinkId, TagLinks.FilePath AS [FilePath], TagLinks.TagId
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

            HttpContext.Current.Cache.Insert(cacheKey, tagLinks, null, DateTime.Today.AddDays(1), TimeSpan.Zero);
            CacheKeys.Add(cacheKey);

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

        /// <summary>
        ///     Creates multiple tag links at once. For performance reasons, the ID is not set.
        /// </summary>
        /// <param name="tagLinks"></param>
        /// <returns></returns>
        public static bool CreateRange(IEnumerable<TagLink> tagLinks)
        {
            ICollection<SqliteParameter> parameters = new List<SqliteParameter>();

            string query = "INSERT INTO TagLinks (TagId, FilePath) VALUES";

            int tagLinkIndex = 1;
            bool isSuccess = true;
            foreach (TagLink tagLink in tagLinks)
            {
                // We may need to search for paths later and we don't want to be dealing with differences in slashes
                tagLink.FilePath = tagLink.FilePath.ToLower().Replace("/", "\\");

                parameters.Add(SqliteWrapper.GenerateParameter($@"TagId{tagLinkIndex}", tagLink.TagId));
                parameters.Add(SqliteWrapper.GenerateParameter($"FilePath{tagLinkIndex}", tagLink.FilePath));

                query += $" (@TagId{tagLinkIndex}, @FilePath{tagLinkIndex}),";

                // Quick fix for "too many SQL variables" as I'm tired. // Todo - Refactor this
                if (tagLinkIndex % 499 == 0)
                {
                    query = query.TrimEnd(',');

                    using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
                    {
                        isSuccess &= dbContext.ExecuteNonQuery(query, parameters);
                    }

                    parameters = new List<SqliteParameter>();
                    query = "INSERT INTO TagLinks (TagId, FilePath) VALUES";
                }

                tagLinkIndex++;
            }

            if (query == "INSERT INTO TagLinks (TagId, FilePath) VALUES")
            {
                return isSuccess;
            }

            query = query.TrimEnd(',');

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                isSuccess &= dbContext.ExecuteNonQuery(query, parameters);
            }

            ClearCaches();

            return isSuccess;
        }

        private static void ClearCaches()
        {
            foreach (string cacheKey in CacheKeys)
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }
        }

        public bool Create()
        {
            // We may need to search for paths later and we don't want to be dealing with differences in slashes
            FilePath = FilePath.ToLowerInvariant().Replace("/", "\\");

            const string query =
                @"INSERT INTO TagLinks (TagId, FilePath) VALUES (@TagId, @FilePath); SELECT last_insert_rowid()";

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                TagLinkId = Convert.ToInt32(dbContext.GetScalar(query, GenerateParameters()));
            }

            bool isSuccess = TagLinkId != 0;

            ClearCaches();

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

            ClearCaches();

            return isSuccess;
        }

        public bool Update()
        {
            // We may need to search for paths later and we don't want to be dealing with differences in slashes
            FilePath = FilePath.ToLowerInvariant().Replace("/", "\\");

            const string query =
                @"UPDATE TagLinks SET TagId = @TagId, FilePath = @FilePath WHERE TagLinkId = @TagLinkId";

            using (SqliteWrapper dbContext = new SqliteWrapper("AppDb"))
            {
                dbContext.GetScalar(query, GenerateParameters());
            }

            bool isSuccess = TagLinkId != 0;

            ClearCaches();

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