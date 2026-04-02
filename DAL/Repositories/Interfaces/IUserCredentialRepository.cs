using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface IUserCredentialRepository : IRepository<UserCredential>
    {
        Task<UserCredential?> GetByUserIdAsync(long userId);
        Task<UserCredential?> GetByUserNameAsync(string username);
    }
}