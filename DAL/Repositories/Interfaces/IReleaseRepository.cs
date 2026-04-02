using DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface IReleaseRepository : IRepository<Release>
    {
        Task<List<Release>> GetAllWithDetailsAsync();
        Task<Release?> GetWithScriptsAsync(long id);
        Task<Release?> GetByVersionAsync(string version);

    }
}