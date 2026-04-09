using DAL.Common;
using DAL.Enums;

namespace DAL.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Script> Scripts { get; set; } = new List<Script>();
        public ICollection<Release> CreatedReleases { get; set; } = new List<Release>();
        public UserCredential? Credential { get; set; }
        public ICollection<Batch> CreateBatches { get; set; } = new List<Batch>();
        public ICollection<Conflict> ResolvedConflicts { get; set; } = new List<Conflict>();
    }
}