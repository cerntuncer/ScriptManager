using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Enums
{
    public enum ConflictStatus
    {
        Open = 1,//conflict yeni tespit edilmiş henüz çözülmemiş
        Resolved = 2,//conflict çözülmüş
        Ignored = 3//conflict kritik değil
    }
}