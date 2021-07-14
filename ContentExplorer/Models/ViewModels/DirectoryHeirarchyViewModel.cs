namespace ContentExplorer.Models.ViewModels
{
    public class DirectoryHierarchyViewModel
    {
        public string Name { get; set; }
        public string ContentUrl { get; set; }
        public string Path { get; set; }
        public DirectoryHierarchyViewModel Parent { get; set; }
    }
}