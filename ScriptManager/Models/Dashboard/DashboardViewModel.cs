using ScriptManager.Models.Release;

namespace ScriptManager.Models.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalReleases { get; set; }
        public int TotalScripts { get; set; }
        public int OpenConflicts { get; set; }
        public int ReadyScripts { get; set; }
        public int TestingScripts { get; set; }
        public List<ReleaseListItemViewModel> LatestReleases { get; set; } = new();
        public List<BatchSummaryViewModel> RecentBatches { get; set; } = new();
        public List<AlertItemViewModel> Alerts { get; set; } = new();

    }
    public class BatchSummaryViewModel
    {
        public string Name { get; set; }
        public int ScriptCount { get; set; }
    }
    public class AlertItemViewModel
    {
        public int Severity { get; set; }
        public string Message { get; set; }

    }

}