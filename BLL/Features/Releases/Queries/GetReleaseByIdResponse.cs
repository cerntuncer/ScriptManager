using BLL.Common;

namespace BLL.Features.Releases.Queries
{
    public class GetReleaseByIdResponse : BaseResponse
    {
        public long ReleaseId { get; set; }
        public string Version { get; set; } = string.Empty;

        public List<ReleaseScriptDto> Scripts { get; set; } = new();

        public string CombinedSql { get; set; } = string.Empty;
        public string CombinedRollback { get; set; } = string.Empty;
    }

    public class ReleaseScriptDto
    {
        public long ScriptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
