using ScriptManager.Models.Script;

namespace ScriptManager.Models.Release;

public class ReleasesIndexViewModel
{
    public List<ReleaseListItemViewModel> Releases { get; set; } = new();
    public List<UserOptionViewModel> Developers { get; set; } = new();
}
