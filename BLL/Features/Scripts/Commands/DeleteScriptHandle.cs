using BLL.Services;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Scripts.Commands
{
    public class DeleteScriptHandle : IRequestHandler<DeleteScriptRequest, DeleteScriptResponse>
    {
        private readonly MyContext _db;
        private readonly IScriptConflictSyncService _conflictSync;

        public DeleteScriptHandle(MyContext db, IScriptConflictSyncService conflictSync)
        {
            _db = db;
            _conflictSync = conflictSync;
        }

        public async Task<DeleteScriptResponse> Handle(DeleteScriptRequest request, CancellationToken cancellationToken)
        {
            var script = await _db.Scripts
                .FirstOrDefaultAsync(s => s.Id == request.ScriptId && !s.IsDeleted, cancellationToken);

            if (script == null)
            {
                return new DeleteScriptResponse { Success = false, Message = "Script bulunamadı." };
            }

            if (script.Status == ScriptStatus.Deleted)
            {
                return new DeleteScriptResponse { Success = false, Message = "Script zaten silinmiş." };
            }

            var id = script.Id;
            var conflictRows = await _db.Conflicts
                .Where(c => c.ScriptId == id || c.ConflictingScriptId == id)
                .ToListAsync(cancellationToken);
            var recomputeIds = conflictRows
                .SelectMany(c => new[] { c.ScriptId, c.ConflictingScriptId })
                .Where(x => x != id)
                .ToHashSet();

            _db.Conflicts.RemoveRange(conflictRows);

            script.Status = ScriptStatus.Deleted;
            script.UpdatedAt = DateTime.UtcNow;
            _db.Scripts.Update(script);

            await _db.SaveChangesAsync(cancellationToken);

            foreach (var oid in recomputeIds)
                await _conflictSync.RecomputeScriptStatusAsync(oid);

            return new DeleteScriptResponse
            {
                Success = true,
                Message = "Script silindi.",
                ScriptId = script.Id
            };
        }
    }
}
