using BLL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class MoveScriptToBatchResponse : BaseResponse
    {
        public long ScriptId { get; set; }
        public long BatchId { get; set; }
    }
}