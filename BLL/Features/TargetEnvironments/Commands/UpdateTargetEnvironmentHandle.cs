using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.TargetEnvironments.Commands
{
    public class UpdateTargetEnvironmentHandle
        : IRequestHandler<UpdateTargetEnvironmentRequest, TargetEnvironmentCommandResponse>
    {
        private readonly ITargetEnvironmentRepository _repo;

        public UpdateTargetEnvironmentHandle(ITargetEnvironmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<TargetEnvironmentCommandResponse> Handle(
            UpdateTargetEnvironmentRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ProjectName))
                return TargetEnvironmentCommandResponse.Fail("Proje adı boş olamaz.");

            if (string.IsNullOrWhiteSpace(request.ConnectionString))
                return TargetEnvironmentCommandResponse.Fail("Bağlantı dizesi boş olamaz.");

            var entity = await _repo.GetByIdAsync(request.Id);
            if (entity == null)
                return TargetEnvironmentCommandResponse.Fail("Hedef ortam bulunamadı.");

            // Check uniqueness only if project name or environment type changed
            if (!string.Equals(entity.ProjectName, request.ProjectName.Trim(), StringComparison.OrdinalIgnoreCase)
                || entity.EnvironmentType != request.EnvironmentType)
            {
                var duplicate = await _repo.GetByProjectAndEnvironmentAsync(
                    request.ProjectName.Trim(), request.EnvironmentType);
                if (duplicate != null && duplicate.Id != request.Id)
                    return TargetEnvironmentCommandResponse.Fail(
                        $"\"{request.ProjectName}\" projesi için {request.EnvironmentType} ortamı zaten kayıtlı.");
            }

            entity.ProjectName = request.ProjectName.Trim();
            entity.EnvironmentType = request.EnvironmentType;
            entity.ConnectionString = request.ConnectionString.Trim();
            entity.Description = request.Description?.Trim();

            _repo.Update(entity);
            await _repo.SaveAsync();

            return TargetEnvironmentCommandResponse.Ok(entity.Id, "Hedef ortam güncellendi.");
        }
    }
}
