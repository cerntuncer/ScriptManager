using BLL.Common;

namespace BLL.Features.Releases.Commands
{
    public class CreateReleaseResponse : BaseResponse
    {
        public long ReleaseId { get; set; }
        public string? ReleaseName { get; set; }
        public string? Version { get; set; }
        public string? CreatedAtDisplay { get; set; }
        public int ScriptCount { get; set; }
        public int RollbackScriptCount { get; set; }
        public long? RootBatchId { get; set; }

        public static CreateReleaseResponse Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
