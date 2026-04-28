using DAL.Entities;
using DAL.Enums;

namespace DAL.Repositories.Interfaces
{
    public interface ITargetEnvironmentRepository : IRepository<TargetEnvironment>
    {
        Task<List<TargetEnvironment>> GetByProjectNameAsync(string projectName);
        Task<TargetEnvironment?> GetByProjectAndEnvironmentAsync(string projectName, EnvironmentType environmentType);
        Task<List<TargetEnvironment>> GetAllActiveAsync();
    }
}
