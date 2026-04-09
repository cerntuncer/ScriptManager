using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Base;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Concretes
{
    public class ScriptRepository : BaseRepository<Script>, IScriptRepository
    {
        public ScriptRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<Script>> GetAllDetailedAsync()
        {
            return await _context.Scripts
                .Where(x => !x.IsDeleted && x.Status != ScriptStatus.Deleted)
                .Include(x => x.Batch)
                .Include(x => x.Developer)
                .ToListAsync();
        }

        public async Task<Script?> GetDetailedByIdAsync(long id)
        {
            return await _context.Scripts
                .Include(x => x.Batch)
                .Include(x => x.Developer)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Script>> GetByStatusAsync(ScriptStatus status)
        {
            return await _context.Scripts
                .Where(x => x.Status == status)
                .ToListAsync();
        }

        public async Task<List<Script>> GetByBatchIdAsync(long batchId)
        {
            return await _context.Scripts
                .Where(x => x.BatchId == batchId)
                .Include(x => x.Batch)
                .ToListAsync();
        }
    }
}