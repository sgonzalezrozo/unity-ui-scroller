using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tools.RecyclableScroller.Core.Scripts.Interfaces
{
    public interface IPaginatedDataProvider<T> where T : class
    {
        Task<PaginationResult<T>> LoadPageAsync(int pageNumber);
    }

    public class PaginationResult<T> where T : class
    {
        public bool Success { get; set; }
        public List<T> Items { get; set; }
        public bool HasMorePages { get; set; }

        public PaginationResult()
        {
            Items = new();
        }
    }
}

