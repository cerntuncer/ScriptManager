using MediatR;

namespace BLL.Features.Batchs.Commands
{
    public class CreateBatchRequest : IRequest<CreateBatchResponse>
    {
        public string Name { get; set; } = string.Empty;
        public long CreatedBy { get; set; }
        public long ParentBatchId { get; set; }
        public long? ReleaseId { get; set; }
    }
}