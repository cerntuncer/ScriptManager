using BLL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptResponse : BaseResponse
    {
        public int ScriptId { get; set; }
        public int BatchId { get; set; }
    }
}