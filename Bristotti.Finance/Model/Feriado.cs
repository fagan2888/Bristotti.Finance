using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bristotti.Finance.Model
{
    public class Holiday : Entity
    {
        public virtual DateTime Date { get; set; }
        public virtual string Name { get; set; }
    }
}
