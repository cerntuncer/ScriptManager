using DAL.Common;
using DAL.Enums;

namespace DAL.Entities
{
    public class Conflict : BaseEntity
    {
        public long ScriptId { get; set; }//ilk script
        public long ConflictingScriptId { get; set; }//çakışan ikinci script
        public string TableName { get; set; } = null!;
        public ConflictSeverity Severity { get; set; } = ConflictSeverity.ReviewAdvised;
        public DateTime DetectedAt { get; set; }//conflict tespit zamanı
        public long? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public ConflictCloseReason? CloseReason { get; set; }

        public string? ResolvedSqlHashScript { get; set; }

        public string? ResolvedSqlHashConflictingScript { get; set; }

        public Script Script { get; set; } = null!;
        public Script ConflictingScript { get; set; } = null!;
        public User? ResolvedByUser { get; set; } = null!;



    }
}