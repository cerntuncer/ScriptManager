namespace ScriptManager.Models.Release;

public class ExportSelectedSqlRequest
{
    public long ReleaseId { get; set; }
    public List<long>? ScriptIds { get; set; }
}

public class ReleaseDeleteBody
{
    public long ReleaseId { get; set; }
}

public class BulkDeleteReleasesRequest
{
    public List<long>? ReleaseIds { get; set; }
}
