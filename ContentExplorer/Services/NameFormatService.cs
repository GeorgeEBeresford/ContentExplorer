using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ContentExplorer.Services
{
    public class NameFormatService
    {
        public FileInfo FormatFileName(FileInfo fileInfo)
        {
            string formattedFileName = HttpUtility.UrlDecode(fileInfo.Name);

            formattedFileName = formattedFileName
                .Replace("\\", " ")
                .Replace("/", " ")
                .Replace(":", " - ");

            string[] wordsToRemoveFromNames = ConfigurationManager.AppSettings["WordsToRemoveFromNames"]?.Split('|');

            if (wordsToRemoveFromNames != null)
            {
                foreach (string wordToRemoveFromNames in wordsToRemoveFromNames)
                {
                    formattedFileName = formattedFileName.Replace($"{wordToRemoveFromNames} ", "");
                }
            }

            // Max length
            bool isTooLong = fileInfo.FullName.Length >= 250;
            if (isTooLong)
            {
                formattedFileName = formattedFileName.Remove(50);
                formattedFileName += fileInfo.Extension;
            }

            string formattedFullName = Path.Combine(fileInfo.Directory.FullName, formattedFileName);

            if (fileInfo.Name != formattedFileName)
            {
                try
                {
                    if (new FileInfo(formattedFullName).Exists)
                    {
                        formattedFileName = $"{formattedFileName}~{Guid.NewGuid()}";
                        formattedFullName = Path.Combine(fileInfo.Directory.FullName, formattedFileName + fileInfo.Extension);
                    }
                }
                catch (NotSupportedException)
                {
                    throw new NotSupportedException($"The file path {formattedFullName} is not supported by FileInfo.Exists");
                }

                string oldPath = isTooLong != true ? fileInfo.FullName : $@"\\?\{fileInfo.FullName}";

                try
                {
                    File.Move(oldPath, formattedFullName);
                }
                catch (DirectoryNotFoundException)
                {
                    throw new DirectoryNotFoundException($"Could not find either directory {oldPath} or directory {formattedFullName}");
                }
            }

            FileInfo renamedFile = new FileInfo(formattedFullName);
            return renamedFile;
        }
    }
}