namespace ScriptManager.Models.Script;

public class ChangeScriptStatusFormRequest
{
    public long ScriptId { get; set; }

    /// <summary><see cref="DAL.Enums.ScriptStatus"/> sayısal değeri (Testing=2, Ready=3).</summary>
    public int NewStatus { get; set; }
}
