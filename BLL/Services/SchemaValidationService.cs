using DAL.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace BLL.Services;

public class SchemaValidationService : ISchemaValidationService
{
    private readonly ITargetEnvironmentRepository _targetEnvRepo;

    public SchemaValidationService(ITargetEnvironmentRepository targetEnvRepo)
    {
        _targetEnvRepo = targetEnvRepo;
    }

    public async Task<SchemaValidationResult> ValidateAsync(
        string? sqlScript,
        string? rollbackScript,
        long targetEnvironmentId,
        CancellationToken cancellationToken = default)
    {
        var targetEnv = await _targetEnvRepo.GetByIdAsync(targetEnvironmentId);
        if (targetEnv == null)
            return Fail($"Hedef ortam bulunamadı (ID: {targetEnvironmentId}).");

        // Baştaki/sondaki tırnak ve fazladan backslash'leri temizle
        var connectionString = targetEnv.ConnectionString
            .Trim()
            .Trim('"', '\'')
            .Replace("\\\\", "\\");

        // Extract all referenced tables
        var tables = SqlReferencedTableExtractor.ExtractTables(sqlScript);
        tables.UnionWith(SqlReferencedTableExtractor.ExtractTables(rollbackScript));

        // Extract columns per table (merge script + rollback)
        var columnMap = MergeColumnMaps(
            SqlReferencedColumnExtractor.ExtractColumns(sqlScript),
            SqlReferencedColumnExtractor.ExtractColumns(rollbackScript));

        if (tables.Count == 0)
            return new SchemaValidationResult { IsValid = true };

        try
        {
            var tableResults = await ValidateAgainstDatabaseAsync(
                connectionString, tables, columnMap, cancellationToken);

            var isValid = tableResults.All(t => t.ExistsInDatabase)
                          && tableResults.SelectMany(t => t.ScriptColumns).All(c => c.ExistsInDatabase);

            return new SchemaValidationResult
            {
                IsValid = isValid,
                Tables = tableResults
            };
        }
        catch (Exception ex)
        {
            return Fail($"Hedef veritabanına bağlanılamadı: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Database operations
    // ──────────────────────────────────────────────────────────────────

    private static async Task<List<TableValidationResult>> ValidateAgainstDatabaseAsync(
        string connectionString,
        HashSet<string> tables,
        Dictionary<string, HashSet<string>> columnMap,
        CancellationToken ct)
    {
        var results = new List<TableValidationResult>();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var tableFullName in tables)
        {
            var (schema, tableName) = ParseTableName(tableFullName);

            var tableExists = await CheckTableExistsAsync(conn, schema, tableName, ct);

            if (!tableExists)
            {
                results.Add(new TableValidationResult
                {
                    TableName = tableFullName,
                    ExistsInDatabase = false
                });
                continue;
            }

            var dbColumns = await GetTableColumnsAsync(conn, schema, tableName, ct);

            // Find which script columns match this table (try full name and bare name)
            columnMap.TryGetValue(tableFullName, out var scriptCols);
            if (scriptCols == null)
                columnMap.TryGetValue(tableName, out scriptCols);

            var columnResults = new List<ColumnValidationResult>();
            if (scriptCols != null)
            {
                foreach (var col in scriptCols)
                {
                    var match = dbColumns.FirstOrDefault(c =>
                        string.Equals(c.Name, col, StringComparison.OrdinalIgnoreCase));

                    columnResults.Add(new ColumnValidationResult
                    {
                        ColumnName = col,
                        ExistsInDatabase = match != default,
                        ActualDataType = match == default ? null : match.DataType
                    });
                }
            }

            results.Add(new TableValidationResult
            {
                TableName = tableFullName,
                ExistsInDatabase = true,
                ScriptColumns = columnResults,
                DatabaseColumns = dbColumns
                    .Select(c => $"{c.Name} ({c.DataType})")
                    .ToList()
            });
        }

        return results;
    }

    private static async Task<bool> CheckTableExistsAsync(
        SqlConnection conn, string schema, string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE (@schema = '%' OR TABLE_SCHEMA = @schema)
              AND TABLE_NAME = @table
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<List<(string Name, string DataType)>> GetTableColumnsAsync(
        SqlConnection conn, string schema, string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT COLUMN_NAME, DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE (@schema = '%' OR TABLE_SCHEMA = @schema)
              AND TABLE_NAME = @table
            ORDER BY ORDINAL_POSITION
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);

        var cols = new List<(string Name, string DataType)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            cols.Add((reader.GetString(0), reader.GetString(1)));

        return cols;
    }

    // ──────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────

    private static (string Schema, string TableName) ParseTableName(string fullName)
    {
        var parts = fullName.Split('.', 2);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : ("%", parts[0]);
    }

    private static Dictionary<string, HashSet<string>> MergeColumnMaps(
        Dictionary<string, HashSet<string>> a,
        Dictionary<string, HashSet<string>> b)
    {
        foreach (var (table, cols) in b)
        {
            if (a.TryGetValue(table, out var existing))
                existing.UnionWith(cols);
            else
                a[table] = cols;
        }
        return a;
    }

    private static SchemaValidationResult Fail(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
