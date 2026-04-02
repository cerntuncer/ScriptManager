using DAL.Context;
using DAL.Entities;
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
    public class ReleaseScriptRepository : BaseRepository<ReleaseScript>, IReleaseScriptRepository
    {
        public ReleaseScriptRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<ReleaseScript>> GetByReleaseIdAsync(long releaseId)
        {
            return await _context.ReleaseScripts
                .Where(x => x.ReleaseId == releaseId)
                .Include(x => x.Script)
                .ToListAsync();
        }

        public async Task<List<ReleaseScript>> GetOrderedScriptsAsync(long releaseId)
        {
            return await _context.ReleaseScripts
                .Where(x => x.ReleaseId == releaseId)
                .OrderBy(x => x.ExecutionOrder)
                .Include(x => x.Script)
                .ToListAsync();
        }
    }
}