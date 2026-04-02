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
    public class CommitRepository : BaseRepository<Commit>, ICommitRepository
    {
        public CommitRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<Commit>> GetByScriptIdAsync(long scriptId)
        {
            return await _context.Commits
                .Where(x => x.ScriptId == scriptId)
                .Include(x => x.User)
                .ToListAsync();
        }

        public async Task<List<Commit>> GetByUserIdAsync(long userId)
        {
            return await _context.Commits
                .Where(x => x.UserId == userId)
                .Include(x => x.Script)
                .ToListAsync();
        }
    }
}