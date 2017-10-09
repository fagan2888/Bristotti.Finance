using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Finance.Model;

namespace Bristotti.Finance
{
    public interface IDI1Repository : IDisposable
    {
        IList<DI1> GetByMarketDate(DateTime date);
    }

    public class YieldCurveBrz
    {
        private readonly IHolidayRepository _feriadoRepository;
        private readonly IDI1Repository _di1Repository;
        private readonly ICopomRepository _copomRepository;
        private readonly HolidaySerie _holidaySerie;

        private YieldCurveBrz(IHolidayRepository feriadoRepository, IDI1Repository di1Repository,
            ICopomRepository copomRepository)
        {
            _feriadoRepository = feriadoRepository;
            _di1Repository = di1Repository;
            _copomRepository = copomRepository;

            _holidaySerie = new HolidaySerie(feriadoRepository.GetFeriados("CDI"));
        }

        public YieldInterpol[] Interpolate(DateTime date)
        {
            var di = (from c in _di1Repository.GetByMarketDate(date)
                select new YieldInterpol
                {
                    T = c.BusinessDaysToMaturity,
                    R = c.Close,
                    IsCopom = false
                }).ToArray();

            var copom = (from c in _copomRepository.GetMeetings()
                where c.MeetingDate >= date
                select new YieldInterpol
                {
                    T = _holidaySerie.GetBusinessDays(date, c.MeetingDate),
                    IsCopom = true
                }).ToArray();

            var table = di.Union(copom).OrderBy(x => x.T).ToArray();

            for (int i = 0; i < table.Length; i++)
            {
                var y = table[i];

                if (i == 0)
                    y.DeltaT = y.T;
            }

            return table;
        }
    }

    public class YieldInterpol
    {
        public double T { get; set; }
        public double DeltaT { get; set; }
        public double R { get; set; }
        public bool IsCopom { get; set; }
        public bool SameFwd { get; set; }
        public double FwdInput { get; set; }
        public double Fwd { get; set; }
        public double Spot { get; set; }
        public double Rm_R { get; set; }
    }

    public class HolidaySerie
    {
        private readonly HashSet<DateTime> _holidays;

        public HolidaySerie(IEnumerable<Holiday> holidays)
        {
            _holidays = new HashSet<DateTime>(holidays.Select(x => x.Date));
        }

        public int GetBusinessDays(DateTime from, DateTime to)
        {
            var days = 0;

            while (from < to)
            {
                if (from.DayOfWeek != DayOfWeek.Sunday
                    && from.DayOfWeek == DayOfWeek.Saturday
                    && _holidays.Contains(from))
                    days++;

                from = from.AddDays(1);
            }

            return days;
        }

        public DateTime GetNextBusinessDate(DateTime date, int days)
        {
            throw new InvalidOperationException("kkk");
        }
    }

    public class CurvePoint
    {
        public int Term { get; set; }
        public double Interest  { get; set; }
    }
}
