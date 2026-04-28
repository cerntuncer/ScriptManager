using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.TargetEnvironments.Commands
{
    public class DeleteTargetEnvironmentHandle
        : IRequestHandler<DeleteTargetEnvironmentRequest, TargetEnvironmentCommandResponse>
    {
        private readonly ITargetEnvironmentRepository _repo;

        public DeleteTargetEnvironmentHandle(ITargetEnvironmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<TargetEnvironmentCommandResponse> Handle(
            DeleteTargetEnvironmentRequest request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id);
            if (entity == null)
                return TargetEnvironmentCommandResponse.Fail("Hedef ortam bulunamadı.");

            await _repo.SoftDeleteAsync(request.Id);
            await _repo.SaveAsync();

            return TargetEnvironmentCommandResponse.Ok(request.Id, "Hedef ortam silindi.");
        }
    }
}
