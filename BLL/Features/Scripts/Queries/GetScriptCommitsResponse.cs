using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class GetScriptCommitsResponse
    {
        public long CommitId { get; set; }

        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}