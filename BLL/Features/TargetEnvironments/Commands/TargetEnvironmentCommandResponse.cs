namespace BLL.Features.TargetEnvironments.Commands
{
    public class TargetEnvironmentCommandResponse
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public long? TargetEnvironmentId { get; init; }

        public static TargetEnvironmentCommandResponse Ok(long id, string message) =>
            new() { Success = true, TargetEnvironmentId = id, Message = message };

        public static TargetEnvironmentCommandResponse Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
