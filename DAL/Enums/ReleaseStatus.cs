using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Enums
{
    public enum ReleaseStatus
    {
        Draft = 1,//release oluşturulmuş ama hazır değil
        Ready = 2,
        Released = 3,//release production ortamına uygulanmış
        Deprecated = 4//bu release artık kullanılmıyır->conflict çıkan relesade yeni release oluşturulmuş
    }
}