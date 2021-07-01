using System.Collections.Generic;

namespace ContentExplorer.Models.ViewModels
{
    public class PaginatedViewModel<TPaginatedItems>
    {
        public int Total { get; set; }
        public ICollection<TPaginatedItems> CurrentPage { get; set; }
    }
}