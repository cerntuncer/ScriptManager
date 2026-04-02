using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface IBatchRepository : IRepository<Batch>
    {
        Task<List<Batch>> GetWithScriptsAsync();
        Task<Batch?> GetWithScriptsByIdAsync(long id);
    }
}