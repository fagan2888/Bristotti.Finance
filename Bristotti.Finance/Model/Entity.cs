using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bristotti.Finance.Model
{
    public class Entity
    {
        public virtual Guid Id { get; set; }
        public virtual int Version { get; set; }
    }
}
