using ScriptManager.Models.Release;

namespace ScriptManager.Models.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalReleases { get; set; }
        public int TotalScripts { get; set; }
        /// <summary>Açık çakışma kayıtları (Conflicts tablosu); üst çubuk rozeti ile aynı.</summary>
        public int OpenConflicts { get; set; }
        /// <summary>Açık çakışmaya dahil script sayısı (dağılım grafiği; kayıt sayısından farklı olabilir).</summary>
        public int ScriptsInOpenConflict { get; set; }
        public int ReadyScripts { get; set; }
        public int DraftScripts { get; set; }
        public int OtherScripts { get; set; }
        public List<ReleaseListItemViewModel> LatestReleases { get; set; } = new();
    }
}
