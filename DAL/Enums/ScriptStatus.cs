using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Enums
{
    public enum ScriptStatus
    {
        Draft = 1,//script yeni oluşturulmuş ama test edilmemiş
        Testing = 2,
        Ready = 3,//script tamamen hazır conflict yok release e eklenebilir
        Conflict = 4,//script başka scriptle çakışmaktadır
        Deleted = 5

    }
}