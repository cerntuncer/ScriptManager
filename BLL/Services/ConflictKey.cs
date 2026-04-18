namespace BLL.Services;

/// <summary>
/// SQL conflict tespitinde kullanılan anahtar tipleri.
/// </summary>
public enum ConflictKeyType
{
    /// <summary>WHERE koşulunda belirli bir satır hedeflenmiş (WHERE UserId = 5 gibi).</summary>
    Record = 1,

    /// <summary>Tablo üzerinde genel DML (INSERT / UPDATE / DELETE / MERGE) — kayıt özelleştirilmemiş.</summary>
    Dml = 2,

    /// <summary>Tablo yapısal değişikliği: ALTER TABLE, CREATE TABLE, DROP TABLE, TRUNCATE TABLE.</summary>
    TableDdl = 3,

    /// <summary>Veritabanı nesnesi (Stored Procedure, View, Function) oluşturma veya değiştirme.</summary>
    ObjectDdl = 4,
}

/// <summary>
/// Bir SQL scriptinden çıkarılan conflict tespiti anahtarı.
/// <para>
/// İki script arasında çakışma olup olmadığını <see cref="DoConflict"/> ile kontrol edin.
/// </para>
/// </summary>
/// <param name="Type">Conflict anahtar tipi.</param>
/// <param name="ObjectName">Büyük harfle normalize edilmiş nesne adı (tablo, proc, view…).</param>
/// <param name="SubKey">Record çakışmalarında değer bilgisi (ör. "42"). Diğer tipler için null.</param>
public sealed record ConflictKey(ConflictKeyType Type, string ObjectName, string? SubKey = null)
{
    // ─── Factory Metotlar ────────────────────────────────────────────────────

    /// <summary>Belirli bir satır hedefleyen key üretir. Örn: WHERE UserId = 42</summary>
    public static ConflictKey ForRecord(string colName, string value) =>
        new(ConflictKeyType.Record, colName.ToUpperInvariant(), value);

    /// <summary>Tablo yapısal değişikliği (ALTER/CREATE/DROP/TRUNCATE TABLE).</summary>
    public static ConflictKey ForTableDdl(string tableName) =>
        new(ConflictKeyType.TableDdl, tableName.ToUpperInvariant());

    /// <summary>Genel DML (INSERT / UPDATE / DELETE / MERGE) — kayıt özelleştirilmemiş.</summary>
    public static ConflictKey ForDml(string tableName) =>
        new(ConflictKeyType.Dml, tableName.ToUpperInvariant());

    /// <summary>Stored Procedure, View veya Function oluşturma/değiştirme.</summary>
    public static ConflictKey ForObjectDdl(string objectName) =>
        new(ConflictKeyType.ObjectDdl, objectName.ToUpperInvariant());

    // ─── Çakışma Kuralları ──────────────────────────────────────────────────

    /// <summary>
    /// İki conflict key'inin gerçek bir çakışma oluşturup oluşturmadığını belirler.
    /// <list type="table">
    ///   <listheader><term>A Tipi</term><term>B Tipi</term><term>Çakışır?</term></listheader>
    ///   <item><term>Record</term><term>Record</term><term>Evet — aynı nesne + aynı SubKey</term></item>
    ///   <item><term>TableDdl</term><term>TableDdl</term><term>Evet — aynı tablo yapısal değişiklik</term></item>
    ///   <item><term>TableDdl</term><term>Dml</term><term>Evet — şema + veri değişikliği</term></item>
    ///   <item><term>ObjectDdl</term><term>ObjectDdl</term><term>Evet — aynı nesne</term></item>
    ///   <item><term>Dml</term><term>Dml</term><term>Hayır — çok geniş, gürültü olur</term></item>
    /// </list>
    /// </summary>
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

    /// <summary>
    /// Conflict çiftinden DB'de saklanacak canonical anahtar stringi üretir.
    /// DDL tarafı her zaman öne çıkar (daha anlamlı).
    /// </summary>
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

    /// <summary>DB'de <c>Conflict.TableName</c> sütununa yazılacak string.</summary>
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

    /// <summary>
    /// DB'de saklanan serializasyon stringinden okunabilir kullanıcı etiketi üretir.
    /// Örn: "RECORD:USERID:42" → "Kayıt: UserId = 42"
    ///      "DDL:USERS"        → "Tablo: Users"
    ///      "OBJ:GETUSERS"     → "Nesne: GetUsers"
    /// </summary>
    public static string ToDisplayLabel(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized)) return serialized ?? "";

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
            "DML"    => $"DML: {obj}",
            _        => serialized
        };
    }
}
