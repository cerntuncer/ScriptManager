using DAL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class ReleaseScript : BaseEntity
    {
        public long ReleaseId { get; set; }
        public long ScriptId { get; set; }
        public int ExecutionOrder { get; set; }
        public Release Release { get; set; } = null!;
        public Script Script { get; set; } = null!;
    }
}