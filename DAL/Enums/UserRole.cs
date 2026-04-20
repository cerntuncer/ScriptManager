using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Enums
{
    /// <summary>
    /// Uygulamada yalnızca bu iki rol vardır; ayrı bir yönetici rolü yoktur.
    /// </summary>
    public enum UserRole
    {
        Developer = 1,
        /// <summary>QA: herhangi bir geliştiricinin taslak scriptini test sonrası Hazır yapabilir.</summary>
        Tester = 3,
    }
}