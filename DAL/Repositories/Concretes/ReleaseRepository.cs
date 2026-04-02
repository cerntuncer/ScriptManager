using DAL.Context;
using DAL.Entities;
using DAL.Repositories.Base;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Repositories.Concretes
{
    public class ReleaseRepository : BaseRepository<Release>, IReleaseRepository
    {
        public ReleaseRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<Release>> GetAllWithDetailsAsync()
        {
            return await _context.Releases
                .Include(x => x.ReleaseScripts)
                .ThenInclude(x => x.Script)
                .ToListAsync();
        }

        public async Task<Release?> GetWithScriptsAsync(long id)
        {
            return await _context.Releases
                .Include(x => x.ReleaseScripts)
                .ThenInclude(x => x.Script)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Release?> GetByVersionAsync(string version)
        {
            return await _context.Releases
                .FirstOrDefaultAsync(x => x.Version == version);
        }



    }
}