using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Finance.Model;

namespace Bristotti.Finance.Repositories
{
    public interface IYieldRepository
    {
        IEnumerable<CopomMeeting> GetCopomMeetings(DateTime date);
        IEnumerable<DI1> GetDI1s(DateTime date);
        CDI GetCDI(DateTime date);
        IEnumerable<CDI> GetCDI();
        int GetNetworkDays(DateTime from, DateTime to);
        IEnumerable<Yield> BuildYield(DateTime date);
        HashSet<DateTime> GetHolidays();
    }
}
