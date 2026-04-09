using BLL.Services;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Scripts.Commands
{
    public class UpdateScriptHandle : IRequestHandler<UpdateScriptRequest, UpdateScriptResponse>
    {
        private readonly MyContext _db;
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly IMediator _mediator;
        private readonly IScriptConflictSyncService _conflictSync;

        public UpdateScriptHandle(
            MyContext db,
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository,
            IMediator mediator,
            IScriptConflictSyncService conflictSync)
        {
            _db = db;
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
            _mediator = mediator;
            _conflictSync = conflictSync;
        }

        public async Task<UpdateScriptResponse> Handle(UpdateScriptRequest request, CancellationToken cancellationToken)
        {
            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);
            if (script == null)
                return new UpdateScriptResponse { Success = false, Message = "Script bulunamadı." };

            var hasContent = request.Name != null || request.SqlScript != null || request.RollbackScript != null
                || request.BatchId.HasValue || request.Batch != null;
            if (!hasContent && !request.Status.HasValue)
                return new UpdateScriptResponse { Success = true, Message = "OK.", ScriptId = script.Id };

            var shouldMoveBatch = request.BatchId.HasValue || request.Batch != null;
            Batch? targetBatch = null;

            if (shouldMoveBatch)
            {
                if (request.Batch != null)
                {
                    var created = await _mediator.Send(request.Batch, cancellationToken);
                    if (!created.Success)
                        return new UpdateScriptResponse { Success = false, Message = "Klasör oluşturulamadı: " + created.Message };
                    targetBatch = await _batchRepository.GetByIdAsync(created.BatchId);
                }
                else if (request.BatchId.HasValue)
                    targetBatch = await _batchRepository.GetByIdAsync(request.BatchId.Value);

                if (targetBatch == null)
                    return new UpdateScriptResponse { Success = false, Message = "Klasör bulunamadı." };
                if (targetBatch.IsLocked)
                    return new UpdateScriptResponse { Success = false, Message = "Hedef klasör kilitli." };
                if (request.ReleaseId is long rid)
                {
                    var ok = await _db.Batches.AsNoTracking()
                        .AnyAsync(b => b.Id == targetBatch.Id && !b.IsDeleted && b.ReleaseId == rid, cancellationToken);
                    if (!ok)
                        return new UpdateScriptResponse { Success = false, Message = "Hedef klasör bu sürüme ait değil." };
                }
            }

            if (script.BatchId.HasValue && shouldMoveBatch)
            {
                var cur = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == script.BatchId.Value && !b.IsDeleted, cancellationToken);
                if (cur != null && cur.IsLocked && targetBatch?.Id != cur.Id)
                    return new UpdateScriptResponse { Success = false, Message = "Kilitli klasördeki script taşınamaz." };
            }

            var newStatus = script.Status;
            if (request.Status.HasValue)
            {
                newStatus = (ScriptStatus)request.Status.Value;
                if (newStatus == ScriptStatus.Conflict)
                    return new UpdateScriptResponse { Success = false, Message = "Çakışma durumu yalnızca sistem tarafından atanır." };
                if (newStatus == ScriptStatus.Deleted)
                    return new UpdateScriptResponse { Success = false, Message = "Silme için silme endpoint'ini kullanın." };
            }

            if (request.Name != null)
                script.Name = request.Name;
            if (request.SqlScript != null)
                script.SqlScript = request.SqlScript;
            if (request.RollbackScript != null)
                script.RollbackScript = request.RollbackScript;
            if (shouldMoveBatch)
                script.BatchId = targetBatch?.Id;
            script.Status = newStatus;

            _scriptRepository.Update(script);
            await _scriptRepository.SaveAsync();
            await _conflictSync.SyncAfterScriptSavedAsync(script.Id, cancellationToken);

            return new UpdateScriptResponse { Success = true, Message = "Script güncellendi.", ScriptId = script.Id };
        }
    }
}
