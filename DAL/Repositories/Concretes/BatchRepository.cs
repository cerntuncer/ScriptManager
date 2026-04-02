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
    public class BatchRepository : BaseRepository<Batch>, IBatchRepository
    {
        public BatchRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<Batch>> GetWithScriptsAsync()
        {
            return await _context.Batches
                .Include(x => x.Scripts)
                .ToListAsync();
        }

        public async Task<Batch?> GetWithScriptsByIdAsync(long id)
        {
            return await _context.Batches
                .Include(x => x.Scripts)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

    }
}