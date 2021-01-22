using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace ContentExplorer.Models.ViewModels
{
    public class DirectoryViewModel
    {
        public ICollection<DirectoryInfo> DirectoryInfos { get; set; }
        public ICollection<FileInfo> FileInfos { get; set; }
        public int FileCount { get; set; }
    }
}