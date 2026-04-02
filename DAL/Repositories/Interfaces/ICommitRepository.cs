using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface ICommitRepository : IRepository<Commit>
    {
        Task<List<Commit>> GetByScriptIdAsync(long scriptId);
        Task<List<Commit>> GetByUserIdAsync(long userId);
    }
}