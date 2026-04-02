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
    public class UserCredentialRepository : BaseRepository<UserCredential>, IUserCredentialRepository
    {
        public UserCredentialRepository(MyContext context) : base(context)
        {
        }

        public async Task<UserCredential?> GetByUserIdAsync(long userId)
        {
            return await _context.UserCredentials
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<UserCredential?> GetByUserNameAsync(string username)
        {
            return await _context.UserCredentials
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserName == username);
        }
    }
}