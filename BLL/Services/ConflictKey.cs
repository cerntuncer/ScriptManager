using System.Linq;
using DAL.Enums;

namespace BLL.Services;

public enum ConflictKeyType
{
    Record = 1,

    Dml = 2,

    TableDdl = 3,

    ObjectDdl = 4,
}

public sealed record ConflictKey(ConflictKeyType Type, string ObjectName, string? SubKey = null)
{
    public const string MultiTopicSeparator = " · ";

    // ─── Factory Metotlar ────────────────────────────────────────────────────

    public static ConflictKey ForRecord(string colName, string value) =>
        new(ConflictKeyType.Record, colName.ToUpperInvariant(), value);

    public static ConflictKey ForTableDdl(string tableName) =>
        new(ConflictKeyType.TableDdl, tableName.ToUpperInvariant());

    public static ConflictKey ForDml(string tableName) =>
        new(ConflictKeyType.Dml, tableName.ToUpperInvariant());

    public static ConflictKey ForObjectDdl(string objectName) =>
        new(ConflictKeyType.ObjectDdl, objectName.ToUpperInvariant());

    // ─── Çakışma Kuralları ──────────────────────────────────────────────────

    public static bool DoConflict(ConflictKey a, ConflictKey b)
    {
        if (!string.Equals(a.ObjectName, b.ObjectName, StringComparison.OrdinalIgnoreCase))
            return false;

        return (a.Type, b.Type) switch
        {
            // Aynı satır — SubKey de eşleşmeli
            (ConflictKeyType.Record, ConflictKeyType.Record) =>
                string.Equals(a.SubKey, b.SubKey, StringComparison.OrdinalIgnoreCase),

            // Tablo DDL çakışmaları
            (ConflictKeyType.TableDdl, ConflictKeyType.TableDdl) => true,
            (ConflictKeyType.TableDdl, ConflictKeyType.Dml)      => true,
            (ConflictKeyType.Dml,      ConflictKeyType.TableDdl) => true,

            // Aynı stored proc / view / function
            (ConflictKeyType.ObjectDdl, ConflictKeyType.ObjectDdl) => true,

            _ => false
        };
    }

    public static ConflictSeverity SeverityForPair() => ConflictSeverity.ReviewAdvised;

    public static string CanonicalKey(ConflictKey a, ConflictKey b)
    {
        // DDL daha anlamlı → öne çıkar
        if (a.Type is ConflictKeyType.TableDdl or ConflictKeyType.ObjectDdl)
            return a.Serialize();
        if (b.Type is ConflictKeyType.TableDdl or ConflictKeyType.ObjectDdl)
            return b.Serialize();
        // Record × Record → a (aynı SubKey zaten)
        return a.Serialize();
    }

    // ─── Serializasyon ──────────────────────────────────────────────────────

    public string Serialize() => SubKey is not null
        ? $"{TypeCode()}:{ObjectName}:{SubKey}"
        : $"{TypeCode()}:{ObjectName}";

    private string TypeCode() => Type switch
    {
        ConflictKeyType.Record    => "RECORD",
        ConflictKeyType.Dml       => "DML",
        ConflictKeyType.TableDdl  => "DDL",
        ConflictKeyType.ObjectDdl => "OBJ",
        _                         => "?"
    };

    public static IEnumerable<string> SplitStoredTopics(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) yield break;
        foreach (var part in stored.Split(MultiTopicSeparator, StringSplitOptions.None))
        {
            var t = part.Trim();
            if (t.Length > 0) yield return t;
        }
    }

    public static string CombineTopics(IEnumerable<string> topics)
    {
        var list = topics
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        return string.Join(MultiTopicSeparator, list);
    }

    public static string ToDisplayLabel(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized)) return serialized ?? "";

        var topicParts = SplitStoredTopics(serialized).ToList();
        if (topicParts.Count == 0) return serialized;
        if (topicParts.Count == 1) return ToDisplayLabelSingle(topicParts[0]);

        return string.Join("; ", topicParts.Select(ToDisplayLabelSingle));
    }

    private static string ToDisplayLabelSingle(string serialized)
    {
        var parts = serialized.Split(':', 3);
        if (parts.Length < 2) return serialized;

        var code = parts[0].ToUpperInvariant();
        var obj  = parts[1];
        var sub  = parts.Length > 2 ? parts[2] : null;

        return code switch
        {
            "RECORD" => sub != null ? $"Kayıt: {obj} = {sub}" : $"Kayıt: {obj}",
            "DDL"    => $"Tablo: {obj}",
            "OBJ"    => $"Nesne: {obj}",
            "DML"    => $"Veri değişikliği: {obj}",
            _        => serialized
        };
    }
}
