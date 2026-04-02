using DAL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Batch : BaseEntity
    {
        public string Name { get; set; } = null!;
        public long CreatedBy { get; set; }
        public User Creator { get; set; } = null!;
        public ICollection<Script> Scripts { get; set; } = new List<Script>();

    }
}