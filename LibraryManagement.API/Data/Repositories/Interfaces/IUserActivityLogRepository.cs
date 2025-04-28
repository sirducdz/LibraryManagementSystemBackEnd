using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IUserActivityLogRepository : IRepository<UserActivityLog, long>
    {
    }
}
