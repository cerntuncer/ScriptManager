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
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(MyContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _context.Users
                .AnyAsync(x => x.Email == email);
        }
        public async Task<User?> GetUserWithDetailsAsync(long id)
        {
            return await _context.Users
                .Include(x => x.Scripts)
                .Include(x => x.ResolvedConflicts)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

    }
}