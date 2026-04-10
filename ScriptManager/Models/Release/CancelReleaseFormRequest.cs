namespace ScriptManager.Models.Release;

public class CancelReleaseFormRequest
{
    public long ReleaseId { get; set; }
    public List<long>? ReleaseIds { get; set; }
}
