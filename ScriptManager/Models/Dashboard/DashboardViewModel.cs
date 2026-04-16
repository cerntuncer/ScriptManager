using ScriptManager.Models.Release;

namespace ScriptManager.Models.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalReleases { get; set; }
        public int TotalScripts { get; set; }
        public int OpenConflicts { get; set; }
        public int ReadyScripts { get; set; }
        public int DraftScripts { get; set; }
        public int OtherScripts { get; set; }
        public List<ReleaseListItemViewModel> LatestReleases { get; set; } = new();
    }
}
