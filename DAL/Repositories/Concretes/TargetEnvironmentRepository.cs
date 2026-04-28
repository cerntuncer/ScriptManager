using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Base;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Concretes
{
    public class TargetEnvironmentRepository : BaseRepository<TargetEnvironment>, ITargetEnvironmentRepository
    {
        public TargetEnvironmentRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<TargetEnvironment>> GetByProjectNameAsync(string projectName)
        {
            return await _context.TargetEnvironments
                .Where(x => !x.IsDeleted && x.ProjectName == projectName)
                .OrderBy(x => x.EnvironmentType)
                .ToListAsync();
        }

        public async Task<TargetEnvironment?> GetByProjectAndEnvironmentAsync(string projectName, EnvironmentType environmentType)
        {
            return await _context.TargetEnvironments
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ProjectName == projectName && x.EnvironmentType == environmentType);
        }

        public async Task<List<TargetEnvironment>> GetAllActiveAsync()
        {
            return await _context.TargetEnvironments
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.ProjectName)
                .ThenBy(x => x.EnvironmentType)
                .ToListAsync();
        }
    }
}
