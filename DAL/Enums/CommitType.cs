using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Enums
{
    public enum CommitType
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        ResolveConflict = 4,

        /// <summary>Draft → Testing / Ready veya Testing → Ready durum geçişi.</summary>
        StatusChange = 5
    }
}