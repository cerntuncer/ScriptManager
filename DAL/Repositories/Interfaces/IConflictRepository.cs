using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface IConflictRepository : IRepository<Conflict>
    {
        Task<List<Conflict>> GetByScriptIdAsync(long scriptId);
        Task<List<Conflict>> GetUnresolvedConflictsAsync();
    }
}