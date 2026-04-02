using DAL.Common;
using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Commit : BaseEntity
    {
        public long ScriptId { get; set; }
        public long UserId { get; set; }
        public CommitType Type { get; set; }
        public string? Description { get; set; }
        public Script Script { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}