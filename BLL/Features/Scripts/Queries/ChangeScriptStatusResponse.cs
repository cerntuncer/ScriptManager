using BLL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class ChangeScriptStatusResponse : BaseResponse
    {
        public long ScriptId { get; set; }
        public string Status { get; set; }
    }
}