using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace BLL.Features.Batchs.Commands
{
    public class CreateBatchHandle : IRequestHandler<CreateBatchRequest, CreateBatchResponse>
    {
        private readonly MyContext _db;
        private readonly IBatchRepository _batchRepository;

        public CreateBatchHandle(MyContext db, IBatchRepository batchRepository)
        {
            _db = db;
            _batchRepository = batchRepository;
        }

        public async Task<CreateBatchResponse> Handle(CreateBatchRequest request,
            CancellationToken cancellationToken)
        {
            var name = string.IsNullOrWhiteSpace(request.Name) ? "Batch" : request.Name.Trim();

            Batch batch;
            if (request.ParentBatchId > 0)
            {
                var parent = await _batchRepository.GetActiveByIdAsync(request.ParentBatchId, cancellationToken);
                if (parent == null)
                    return new CreateBatchResponse { Success = false, Message = "Üst batch bulunamadı." };
                if (parent.IsLocked)
                    return new CreateBatchResponse { Success = false, Message = "Kilitli klasöre alt batch eklenemez." };

                var hasScripts = await _db.Scripts.AnyAsync(
                    s => s.BatchId == parent.Id && !s.IsDeleted && s.Status != ScriptStatus.Deleted,
                    cancellationToken);
                if (hasScripts)
                    return new CreateBatchResponse
                        { Success = false, Message = "Script içeren klasöre alt batch eklenemez." };

                batch = new Batch
                {
                    Name = name,
                    ParentBatchId = parent.Id,
                    ReleaseId = parent.ReleaseId,
                    IsLocked = false,
                    CreatedBy = request.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
            }
            else if (request.ReleaseId.HasValue && request.ReleaseId.Value > 0)
            {
                var release = await _db.Releases.FirstOrDefaultAsync(
                    r => r.Id == request.ReleaseId.Value && !r.IsDeleted, cancellationToken);
                if (release == null)
                    return new CreateBatchResponse { Success = false, Message = "Release bulunamadı." };
                if (release.IsCancelled)
                    return new CreateBatchResponse { Success = false, Message = "İptal edilmiş sürüme klasör eklenemez." };

                if (!release.RootBatchId.HasValue)
                {
                    batch = new Batch
                    {
                        Name = name,
                        ParentBatchId = null,
                        ReleaseId = release.Id,
                        IsLocked = false,
                        CreatedBy = request.CreatedBy,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                }
                else
                {
                    var rootBatch =
                        await _batchRepository.GetActiveByIdAsync(release.RootBatchId.Value, cancellationToken);
                    if (rootBatch == null)
                        return new CreateBatchResponse { Success = false, Message = "Kök batch bulunamadı." };
                    if (rootBatch.IsLocked)
                        return new CreateBatchResponse { Success = false, Message = "Bu sürüm kilitli; klasör eklenemez." };

                    batch = new Batch
                    {
                        Name = name,
                        ParentBatchId = release.RootBatchId,
                        ReleaseId = release.Id,
                        IsLocked = false,
                        CreatedBy = request.CreatedBy,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                }
            }
            else
            {
                batch = new Batch
                {
                    Name = name,
                    ParentBatchId = null,
                    ReleaseId = null,
                    IsLocked = false,
                    CreatedBy = request.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
            }

            _db.Batches.Add(batch);
            await _db.SaveChangesAsync(cancellationToken);

            return new CreateBatchResponse
            {
                Success = true,
                Message = "Batch oluşturuldu.",
                BatchId = batch.Id,
                BatchName = batch.Name
            };
        }
    }
}
