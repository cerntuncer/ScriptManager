using DAL.Entities;
using DAL.Enums;

namespace BLL.Services;

public class ScriptWorkflowService : IScriptWorkflowService
{
    private readonly IScriptConflictSyncService _conflicts;

    public ScriptWorkflowService(IScriptConflictSyncService conflicts)
    {
        _conflicts = conflicts;
    }

    public async Task<string?> ValidateTransitionAsync(
        Script script,
        ScriptStatus newStatus,
        User user,
        CancellationToken cancellationToken = default)
    {
        if (script.Status == ScriptStatus.Deleted)
            return "Silinmiş scriptin durumu değiştirilemez.";

        if (newStatus == ScriptStatus.Conflict)
            return "Conflict durumu; çakışma tespit edildiğinde sistem tarafından atanır.";

        if (newStatus == ScriptStatus.Deleted)
            return "Silme işlemi için DeleteScript kullanılmalıdır.";

        if (script.Status == newStatus)
            return "Script zaten bu durumda.";

        var transitionAllowed = (script.Status, newStatus) switch
        {
            (ScriptStatus.Draft, ScriptStatus.Testing) => true,
            (ScriptStatus.Draft, ScriptStatus.Ready) => true,
            (ScriptStatus.Testing, ScriptStatus.Ready) => true,
            _ => false
        };
        if (!transitionAllowed)
        {
            return script.Status == ScriptStatus.Conflict
                ? "Çakışma (Conflict) durumundaki scriptlerde durum değişikliği yapılamaz; önce çakışmayı çözün veya metinleri güncelleyin."
                : "Desteklenen geçişler: Taslak → İncelemede (Test) veya Hazır; İncelemede → Hazır. Conflict durumu sistem tarafından atanır.";
        }

        var hasOpen = await _conflicts.HasUnresolvedConflictsAsync(script.Id, cancellationToken);
        if (newStatus == ScriptStatus.Ready && hasOpen)
            return "Çözülmemiş tablo çakışmaları varken Ready durumuna geçilemez. Çakışma sayfasından kontrol edin veya 'Çakışma yok' ile onaylayın.";

        if (script.Status == ScriptStatus.Conflict && hasOpen)
            return "Açık çakışma kayıtları varken durum değiştirilemez. Önce çakışmayı çözün veya inceleme sonrası kapatın.";

        var isAdmin = user.Role == UserRole.Admin;
        var isDeveloper = user.Role == UserRole.Developer;
        var isTester = user.Role == UserRole.Tester;
        var ownsScript = script.DeveloperId == user.Id;

        switch (newStatus)
        {
            case ScriptStatus.Testing when script.Status == ScriptStatus.Draft:
                if (!isAdmin && !isDeveloper)
                    return "Teste göndermeyi yalnızca geliştirici veya yönetici yapabilir.";
                if (!isAdmin && !ownsScript)
                    return "Yalnızca kendi scriptinizi testçiye gönderebilirsiniz.";
                break;

            case ScriptStatus.Ready when script.Status == ScriptStatus.Draft:
                if (!isAdmin && !isDeveloper)
                    return "Taslaktan doğrudan hazıra geçmeyi yalnızca geliştirici veya yönetici yapabilir (test süreci atlanabilir).";
                if (!isAdmin && !ownsScript)
                    return "Yalnızca kendi scriptinizi hazır durumuna alabilirsiniz.";
                break;

            case ScriptStatus.Ready when script.Status == ScriptStatus.Testing:
                if (!isTester)
                    return "İncelemede (test) olan scripti yalnızca testçi Hazır durumuna alabilir.";
                break;
        }

        return null;
    }

    public Task NormalizeStaleConflictStatusAsync(long scriptId, CancellationToken cancellationToken = default) =>
        _conflicts.RecomputeScriptStatusAsync(scriptId, cancellationToken);
}
