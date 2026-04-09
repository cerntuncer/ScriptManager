using DAL.Common;
using System.Collections.Generic;

namespace DAL.Entities
{
    public class Release : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Version { get; set; } = null!;
        public long CreatedBy { get; set; }
        public bool IsActive { get; set; }

        /// <summary>İptal edildi; paketler havuza döner, sürüm kaydı silinmez.</summary>
        public bool IsCancelled { get; set; }

        public long? RootBatchId { get; set; }
        public Batch? RootBatch { get; set; }
        public User Creator { get; set; } = null!;
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();

    }
}