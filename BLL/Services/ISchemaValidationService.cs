namespace BLL.Services;

public interface ISchemaValidationService
{
    Task<SchemaValidationResult> ValidateAsync(
        string? sqlScript,
        string? rollbackScript,
        long targetEnvironmentId,
        CancellationToken cancellationToken = default);
}

public sealed class SchemaValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<TableValidationResult> Tables { get; init; } = Array.Empty<TableValidationResult>();
}

public sealed class TableValidationResult
{
    public string TableName { get; init; } = "";
    public bool ExistsInDatabase { get; init; }

    /// <summary>Columns referenced in the SQL script for this table.</summary>
    public IReadOnlyList<ColumnValidationResult> ScriptColumns { get; init; } = Array.Empty<ColumnValidationResult>();

    /// <summary>All columns that actually exist in the target DB (name + data type).</summary>
    public IReadOnlyList<string> DatabaseColumns { get; init; } = Array.Empty<string>();
}

public sealed class ColumnValidationResult
{
    public string ColumnName { get; init; } = "";
    public bool ExistsInDatabase { get; init; }
    public string? ActualDataType { get; init; }
}
