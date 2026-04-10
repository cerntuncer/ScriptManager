using BLL.Services;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptHandle : IRequestHandler<CreateScriptRequest, CreateScriptResponse>
    {
        private readonly MyContext _db;
        private readonly IScriptConflictSyncService _conflictSync;
        private readonly ISqlScriptSyntaxValidator _sqlSyntax;

        public CreateScriptHandle(
            MyContext db,
            IScriptConflictSyncService conflictSync,
            ISqlScriptSyntaxValidator sqlSyntax)
        {
            _db = db;
            _conflictSync = conflictSync;
            _sqlSyntax = sqlSyntax;
        }

        public async Task<CreateScriptResponse> Handle(CreateScriptRequest request, CancellationToken cancellationToken)
        {
            var syntaxIssues = new List<SqlScriptSyntaxIssue>();
            var sqlR = _sqlSyntax.Validate(request.SqlScript, "SQL");
            if (!sqlR.IsValid)
                syntaxIssues.AddRange(sqlR.Issues);
            var rbR = _sqlSyntax.Validate(request.RollbackScript, "Rollback");
            if (!rbR.IsValid)
                syntaxIssues.AddRange(rbR.Issues);
            if (syntaxIssues.Count > 0)
            {
                return new CreateScriptResponse
                {
                    Success = false,
                    Message = "T-SQL sözdizimi hataları:\n" + SqlScriptSyntaxValidator.FormatIssueList(syntaxIssues)
                };
            }

            long? finalBatchId = null;
            string? batchNameOut = null;
            long? releaseIdOut = null;
            string? releaseNameOut = null;
            string? versionOut = null;

            if (request.BatchId.HasValue && request.BatchId.Value > 0)
            {
                var bid = request.BatchId.Value;
                var lb = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == bid && !b.IsDeleted, cancellationToken);
                if (lb == null)
                    return new CreateScriptResponse { Success = false, Message = "Batch bulunamadı." };
                if (lb.IsLocked)
                    return new CreateScriptResponse { Success = false, Message = "Bu batch kilitli; script eklenemez." };
                finalBatchId = bid;
                batchNameOut = lb.Name;
                if (lb.ReleaseId.HasValue)
                {
                    var lr = await _db.Releases.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == lb.ReleaseId.Value && !r.IsDeleted, cancellationToken);
                    if (lr != null)
                    {
                        releaseIdOut = lr.Id;
                        releaseNameOut = lr.Name;
                        versionOut = lr.Version;
                    }
                }
            }
            else
            {
                finalBatchId = null;
                batchNameOut = "Atanmamış";
            }

            var dev = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.DeveloperId && !u.IsDeleted, cancellationToken);

            var script = new Script
            {
                Name = request.Name?.Trim() ?? string.Empty,
                SqlScript = request.SqlScript ?? string.Empty,
                RollbackScript = request.RollbackScript,
                BatchId = finalBatchId,
                DeveloperId = request.DeveloperId,
                Status = ScriptStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _db.Scripts.Add(script);
            await _db.SaveChangesAsync(cancellationToken);

            await _conflictSync.SyncAfterScriptSavedAsync(script.Id, cancellationToken);

            return new CreateScriptResponse
            {
                Success = true,
                Message = "Script oluşturuldu.",
                ScriptId = script.Id,
                BatchId = finalBatchId,
                ReleaseId = releaseIdOut,
                ReleaseName = releaseNameOut,
                ReleaseVersion = versionOut,
                BatchName = batchNameOut,
                DeveloperName = dev?.Name ?? string.Empty
            };
        }
    }
}
