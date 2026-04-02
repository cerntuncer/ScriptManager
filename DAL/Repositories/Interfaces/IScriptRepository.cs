using DAL.Entities;
using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface IScriptRepository : IRepository<Script>
    {
        Task<List<Script>> GetAllDetailedAsync();
        Task<Script?> GetDetailedByIdAsync(long id);
        Task<List<Script>> GetByStatusAsync(ScriptStatus status);
        Task<List<Script>> GetByBatchIdAsync(long batchId);
    }
}
