using DAL.Entities;
using DAL.Enums;

namespace BLL.Services;

public interface IScriptWorkflowService
{
    /// <summary>Manuel veya API üzerinden durum değişikliği için doğrulama; null başarılıdır.</summary>
    Task<string?> ValidateTransitionAsync(Script script, ScriptStatus newStatus, User user, CancellationToken cancellationToken = default);

    /// <summary>Conflict durumunda iken çakışma kayıtları kalmadıysa durumu düzeltir.</summary>
    Task NormalizeStaleConflictStatusAsync(long scriptId, CancellationToken cancellationToken = default);
}
