using DAL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Batch : BaseEntity
    {
        public string Name { get; set; } = null!;
        public long? ReleaseId { get; set; }
        public long? ParentBatchId { get; set; }
        public long CreatedBy { get; set; }
        public Release? Release { get; set; }
        public Batch? Parent { get; set; }
        public ICollection<Batch> ChildBatches { get; set; } = new List<Batch>();
        public User Creator { get; set; } = null!;
        public ICollection<Script> Scripts { get; set; } = new List<Script>();

    }
}