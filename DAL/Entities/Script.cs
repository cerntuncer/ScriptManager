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
        public long? BatchId { get; set; }
        public long DeveloperId { get; set; }
        public ScriptStatus Status { get; set; }
        public ScriptStatus? StatusBeforeConflict { get; set; }
        public string SqlScript { get; set; } = null!;
        public string? RollbackScript { get; set; }
        public Batch? Batch { get; set; }
        public User Developer { get; set; } = null!;
        public ICollection<Commit> Commits { get; set; } = new List<Commit>();

    }
}