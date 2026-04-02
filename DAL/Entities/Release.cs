using DAL.Common;
using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Release : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Version { get; set; } = null!;
        public ReleaseStatus Status { get; set; }
        public long CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public User Creator { get; set; } = null!;
        public ICollection<ReleaseScript> ReleaseScripts { get; set; } = new List<ReleaseScript>();

    }
}