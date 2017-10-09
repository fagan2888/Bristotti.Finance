using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bristotti.Finance.Model
{
    public class DI1
    {
        public virtual DateTime MarketDate { get; set; }
        public virtual DateTime MaturityDate { get; set; }
        public virtual string MaturityId { get; set; }
        public virtual double Open { get; set; }
        public virtual double Close { get; set; }
        public virtual double High { get; set; }
        public virtual double Low { get; set; }
        public virtual double Avg { get; set; }
        public virtual double Settlement { get; set; }
        public virtual double SettlementD1 { get; set; }
        public virtual double TotalContracts { get; set; }
        public virtual double BusinessDaysToMaturity { get; set; }
        public virtual double MaturityNumber { get; set; }
    }
}
