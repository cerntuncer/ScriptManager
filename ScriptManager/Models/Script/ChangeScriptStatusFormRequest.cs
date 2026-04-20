namespace ScriptManager.Models.Script;

public class ChangeScriptStatusFormRequest
{
    public long ScriptId { get; set; }

    public int NewStatus { get; set; }
}
