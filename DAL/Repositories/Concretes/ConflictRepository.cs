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
    public class ConflictRepository : BaseRepository<Conflict>, IConflictRepository
    {
        public ConflictRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<Conflict>> GetByScriptIdAsync(long scriptId)
        {
            return await _context.Conflicts
                .Where(x => x.ScriptId == scriptId || x.ConflictingScriptId == scriptId)
                .Include(x => x.Script)
                .Include(x => x.ConflictingScript)
                .ToListAsync();
        }

        public async Task<List<Conflict>> GetUnresolvedConflictsAsync()
        {
            return await _context.Conflicts
                .Where(x => x.Status != ConflictStatus.Resolved)
                .Include(x => x.Script)
                .Include(x => x.ConflictingScript)
                .ToListAsync();
        }
    }
}