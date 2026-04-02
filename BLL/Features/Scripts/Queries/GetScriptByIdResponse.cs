using BLL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class GetScriptByIdResponse : BaseResponse
    {
        public long ScriptId { get; set; }
        public string Name { get; set; }
        public string SqlScript { get; set; }
        public string? RollbackScript { get; set; }

        public long BatchId { get; set; }
        public string BatchName { get; set; }

        public long DeveloperId { get; set; }
        public string DeveloperName { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}