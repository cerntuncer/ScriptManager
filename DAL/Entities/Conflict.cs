using DAL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Conflict : BaseEntity
    {
        public long ScriptId { get; set; }//ilk script
        public long ConflictingScriptId { get; set; }//çakışan ikinci script
        public string TableName { get; set; } = null!;
        public DateTime DetectedAt { get; set; }//conflict tespit zamanı
        public long? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public Script Script { get; set; } = null!;
        public Script ConflictingScript { get; set; } = null!;
        public User? ResolvedByUser { get; set; } = null!;



    }
}