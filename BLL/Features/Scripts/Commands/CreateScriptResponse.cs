using BLL.Common;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptResponse : BaseResponse
    {
        public long ScriptId { get; set; }
        public long? BatchId { get; set; }
        public long? ReleaseId { get; set; }
        public string? ReleaseName { get; set; }
        public string? ReleaseVersion { get; set; }
        public string? BatchName { get; set; }
        public string? DeveloperName { get; set; }
    }
}
