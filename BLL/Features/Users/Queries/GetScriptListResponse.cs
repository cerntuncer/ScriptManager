using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetScriptListResponse
    {
        public long ScriptId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string BatchName { get; set; }
        public string DeveloperName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasRollback { get; set; }

        public bool HasOpenConflict { get; set; }
        public List<string> ConflictingTableNames { get; set; } = new();
    }
}