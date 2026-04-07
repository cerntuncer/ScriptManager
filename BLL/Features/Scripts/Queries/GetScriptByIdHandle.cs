using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class GetScriptByIdHandle : IRequestHandler<GetScriptByIdRequest, GetScriptByIdResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<DAL.Entities.User> _userRepository;
        private readonly IConflictRepository _conflictRepository;

        public GetScriptByIdHandle(
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository,
            IRepository<DAL.Entities.User> userRepository,
            IConflictRepository conflictRepository)
        {
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
            _userRepository = userRepository;
            _conflictRepository = conflictRepository;
        }

        public async Task<GetScriptByIdResponse> Handle(GetScriptByIdRequest request, CancellationToken cancellationToken)
        {
            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);

            if (script == null)
            {
                return new GetScriptByIdResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            Batch? batch = null;
            if (script.BatchId.HasValue)
                batch = await _batchRepository.GetByIdAsync(script.BatchId.Value);
            var user = await _userRepository.GetByIdAsync(script.DeveloperId);

            var conflictRows = await _conflictRepository.GetByScriptIdAsync(script.Id);
            var open = conflictRows.Where(c => c.ResolvedAt == null).ToList();
            var openDtos = open.Select(c =>
            {
                var isPrimary = c.ScriptId == script.Id;
                var other = isPrimary ? c.ConflictingScript : c.Script;
                var otherId = isPrimary ? c.ConflictingScriptId : c.ScriptId;
                var otherName = other?.Name ?? $"#{otherId}";
                return new ScriptOpenConflictDto
                {
                    ConflictId = c.Id,
                    TableName = c.TableName,
                    OtherScriptId = otherId,
                    OtherScriptName = otherName,
                    WarningMessage =
                        $"\"{c.TableName}\" tablosu bu script ile \"{otherName}\" (Id:{otherId}) scriptinde ortak; çakışma olabilir, scriptleri kontrol edin."
                };
            }).ToList();

            var tables = open.Select(c => c.TableName).Distinct().OrderBy(s => s).ToList();
            string? summary = null;
            if (tables.Count > 0)
                summary =
                    $"Bu script şu tablolar için aynı release kapsamındaki diğer scriptlerle çakışma riski taşıyor: {string.Join(", ", tables)}. " +
                    "Scriptleri kontrol edin veya çakışma yoksa çakışma listesinden onaylayın.";

            return new GetScriptByIdResponse
            {
                Success = true,
                Message = "Script getirildi.",

                ScriptId = script.Id,
                Name = script.Name,
                SqlScript = script.SqlScript,
                RollbackScript = script.RollbackScript,

                BatchId = script.BatchId ?? 0,
                BatchName = batch?.Name ?? (script.BatchId.HasValue ? "Bilinmiyor" : "Atanmamış"),

                DeveloperId = script.DeveloperId,
                DeveloperName = user?.Name ?? "Bilinmiyor",

                Status = script.Status.ToString(),
                CreatedAt = script.CreatedAt,
                ConflictSummaryWarning = summary,
                OpenConflicts = openDtos
            };
        }
    }
}