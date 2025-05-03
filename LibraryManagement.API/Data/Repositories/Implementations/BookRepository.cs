using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class BookRepository : Repository<Book, int>, IBookRepository
    {
        public BookRepository(LibraryDbContext context) : base(context)
        {
        }

    }
}
