using DAL.Common;
using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Script : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public long BatchId { get; set; }
        public long DeveloperId { get; set; }
        public ScriptStatus Status { get; set; }
        public string SqlScript { get; set; } = null!;
        public string? RollbackScript { get; set; }
        public Batch Batch { get; set; } = null!;
        public User Developer { get; set; } = null!;
        public ICollection<ReleaseScript> ReleaseScripts { get; set; } = new List<ReleaseScript>();
        public ICollection<Conflict> PrimaryConflicts { get; set; } = new List<Conflict>();//bu scriptin başladığı conflict kayıtları
        public ICollection<Conflict> SecondaryConflicts { get; set; } = new List<Conflict>();//bu scriptin dahil olduğu conflict kayıtları
        public ICollection<Commit> Commits { get; set; } = new List<Commit>();

    }
}