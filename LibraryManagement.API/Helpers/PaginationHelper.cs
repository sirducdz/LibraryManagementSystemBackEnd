namespace LibraryManagement.API.Utilities
{
    public static class PaginationHelper
    {
        // 71 phần từ, một trang 10 phần từ => 70/10+1 = 8 trang
        public static IQueryable<T> Paginate<T>(IQueryable<T> query, int pageIndex, int pageSize)
        {
            return query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
        }
    }
}
