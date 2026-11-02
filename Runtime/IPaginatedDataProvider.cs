using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecyclableScroller.Runtime
{
    public class PaginationResult<T>
    {
        public bool Success { get; set; }
        public List<T> Items { get; set; }
        public bool HasMore { get; set; }
        public int CurrentPage { get; set; }
    }

    public interface IPaginatedDataProvider<T> where T : class
    {
        Task<PaginationResult<T>> LoadPageAsync(int page);
    }
}

