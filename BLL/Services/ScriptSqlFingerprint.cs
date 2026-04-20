using System.Security.Cryptography;
using System.Text;
using DAL.Entities;

namespace BLL.Services;

/// <summary>Çakışma çözümünden sonra aynı SQL ile otomatik yeniden tespiti bastırmak için script içerik özeti.</summary>
public static class ScriptSqlFingerprint
{
    public static string Compute(Script s) => Compute(s.SqlScript, s.RollbackScript);

    public static string Compute(string? sqlScript, string? rollbackScript)
    {
        var a = sqlScript?.ReplaceLineEndings("\n").Trim() ?? "";
        var b = rollbackScript?.ReplaceLineEndings("\n").Trim() ?? "";
        var bytes = Encoding.UTF8.GetBytes(a + "\u001e" + b);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
