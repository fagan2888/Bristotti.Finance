using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Finance.Model;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Bristotti.Finance.DataAccess
{
    public class CopomMeetingMapping : ClassMapping<CopomMeeting>
    {
        public CopomMeetingMapping()
        {
            Id(p => p.Id, m => m.Generator(Generators.GuidComb));
            //Property(p => p.IssueDate);
            //Property(p => p.MaturityDate);
            //Property(p => p.Notional);
            //Property(p => p.Ticker);
            //Property(p => p.CurrencyId);
        }
    }
}
