using System;
using System.Collections.Generic;
using System.Linq;
using LinqToExcel;

namespace ConsoleApp1
{
    public class YieldRepository
    {
        private readonly string _file;
        private ExcelQueryFactory _excel;
        private HashSet<DateTime> _holidays;

        public YieldRepository(string file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        private ExcelQueryFactory Excel => _excel ?? (_excel = new ExcelQueryFactory
        {
            FileName = _file
        });

        public IEnumerable<CopomMeeting> GetCopomMeetings(DateTime date)
        {
            var maxDate = new DateTime(date.Year + 1, 12, 31);

            return from x in Excel.Worksheet<CopomMeeting>("CopomMeeting")
                where x.Date >= date && x.Date <= maxDate
                select x;
        }

        public IEnumerable<DI1> GetDI1s(DateTime date)
        {
            Excel.AddMapping<DI1>(x => x.Spot, "Close");
            return from x in Excel.Worksheet<DI1>("di1")
                where x.MarketDate == date && x.TotalContracts > 0
                select x;
        }

        public CDI GetCDI(DateTime date)
        {
            return (from x in Excel.Worksheet<CDI>("cdi")
                where x.Date == date
                select x).FirstOrDefault();
        }

        public IEnumerable<CDI> GetCDI()
        {
            return Excel.Worksheet<CDI>("cdi");
        }

        public int GetNetworkDays(DateTime from, DateTime to)
        {
            if (_holidays == null)
            {
                _holidays = new HashSet<DateTime>(Excel.Worksheet<Holiday>("holiday").Select(x => x.Date).ToArray());
            }

            if (from > to)
                throw new InvalidOperationException();

            if (from == to)
                return 0;

            int networkDays = 0;
            while (from < to)
            {
                if (!_holidays.Contains(from))
                    networkDays++;

                if(from.DayOfWeek == DayOfWeek.Friday)
                    from = from.AddDays(3);
                else if (from.DayOfWeek == DayOfWeek.Saturday)
                    from = from.AddDays(2);
                else
                    from = from.AddDays(1);
            }
            return networkDays;
        }

        public IEnumerable<Yield> BuildYield(DateTime date)
        {
            var copomMeetings = GetCopomMeetings(date).ToArray();
            var di1s = GetDI1s(date).ToArray();
            var cdi = GetCDI(date);

            return InitializeYield(date, cdi, copomMeetings, di1s);
        }

        private IEnumerable<Yield> InitializeYield(DateTime date, CDI cdi, CopomMeeting[] copomMeetings, DI1[] di1s)
        {
            var i_d = 0;
            var i_c = 0;

            yield return new Yield
            {
                Term = 1,
                Forward = cdi.Media,
                SpotMtm = cdi.Media,
                Spot = cdi.Media,
                YieldType = YieldType.CDI
            };

            while (i_c < copomMeetings.Length && i_d < di1s.Length)
            {
                if (copomMeetings[i_c].Date < di1s[i_d].MaturityDate)
                {
                    yield return new Yield
                    {
                        Term = GetNetworkDays(date, copomMeetings[i_c].Date),
                        YieldType = YieldType.COPOM
                    };
                    i_c++;
                }
                else
                {
                    yield return new Yield
                    {
                        Term = GetNetworkDays(date, di1s[i_d].MaturityDate),
                        YieldType = YieldType.DI1,
                        SpotMtm = di1s[i_d].Spot
                    };
                    i_d++;
                }
            }

            for (; i_c < copomMeetings.Length; i_c++)
                yield return new Yield
                {
                    Term = GetNetworkDays(date, copomMeetings[i_c].Date),
                    YieldType = YieldType.COPOM
                };

            for(;i_d < di1s.Length;i_d++)
                yield return new Yield
                {
                    Term = GetNetworkDays(date, di1s[i_d].MaturityDate),
                    YieldType = YieldType.DI1,
                    SpotMtm = di1s[i_d].Spot
                };
        }
    }
}