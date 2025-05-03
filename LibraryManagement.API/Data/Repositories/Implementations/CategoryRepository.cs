using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class CategoryRepository : Repository<Category, int>, ICategoryRepository
    {
        public CategoryRepository(LibraryDbContext context) : base(context)
        {
        }

    }
}
