namespace BLL.Services;


// T-SQL: mümkünse SQL Server <c>NOEXEC</c> (derleme), aksi halde ScriptDom (GO ile batch'ler).

public interface ISqlScriptSyntaxValidator
{
    /// <param name="labelPrefix">Örn. "SQL" veya "Rollback"</param>
    SqlScriptSyntaxResult Validate(string? sqlText, string labelPrefix);
}

public sealed class SqlScriptSyntaxResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<SqlScriptSyntaxIssue> Issues { get; init; } = Array.Empty<SqlScriptSyntaxIssue>();
}

public sealed class SqlScriptSyntaxIssue
{
    public string Source { get; init; } = "";
    public int BatchNumber { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public string Message { get; init; } = "";
}

//ValidateSql endpoint gövdesi (MVC + API)
public sealed class SqlSyntaxValidationRequest
{
    public string? SqlScript { get; set; }
    public string? RollbackScript { get; set; }
}
