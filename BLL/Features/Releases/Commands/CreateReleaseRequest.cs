using MediatR;

namespace BLL.Features.Releases.Commands
{
    /// <summary>Havuzdaki üst düzey batch ağaçlarını sürüme alır; ağaçlar kilitlenir.</summary>
    public class CreateReleaseRequest : IRequest<CreateReleaseResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public long CreatedBy { get; set; }

        /// <summary>Kullanılmıyor (geriye dönük uyumluluk).</summary>
        public string? InitialBatchName { get; set; }

        /// <summary>Release'e alınacak paket kökleri (havuz ağacındaki seçilen düğüm ve alt ağaçları).</summary>
        public List<long>? SourcePoolBatchRootIds { get; set; }

        public long? RestrictScriptAssignmentToDeveloperId { get; set; }
    }
}
