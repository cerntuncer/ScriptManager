using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.TargetEnvironments.Commands
{
    public class CreateTargetEnvironmentHandle
        : IRequestHandler<CreateTargetEnvironmentRequest, TargetEnvironmentCommandResponse>
    {
        private readonly ITargetEnvironmentRepository _repo;

        public CreateTargetEnvironmentHandle(ITargetEnvironmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<TargetEnvironmentCommandResponse> Handle(
            CreateTargetEnvironmentRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ProjectName))
                return TargetEnvironmentCommandResponse.Fail("Proje adı boş olamaz.");

            if (string.IsNullOrWhiteSpace(request.ConnectionString))
                return TargetEnvironmentCommandResponse.Fail("Bağlantı dizesi boş olamaz.");

            var existing = await _repo.GetByProjectAndEnvironmentAsync(
                request.ProjectName.Trim(), request.EnvironmentType);

            if (existing != null)
                return TargetEnvironmentCommandResponse.Fail(
                    $"\"{request.ProjectName}\" projesi için {request.EnvironmentType} ortamı zaten kayıtlı.");

            var entity = new TargetEnvironment
            {
                ProjectName = request.ProjectName.Trim(),
                EnvironmentType = request.EnvironmentType,
                ConnectionString = request.ConnectionString.Trim(),
                Description = request.Description?.Trim()
            };

            await _repo.AddAsync(entity);
            await _repo.SaveAsync();

            return TargetEnvironmentCommandResponse.Ok(entity.Id, "Hedef ortam oluşturuldu.");
        }
    }
}
