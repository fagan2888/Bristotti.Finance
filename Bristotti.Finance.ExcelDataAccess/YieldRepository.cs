using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bristotti.Finance.Model;
using Bristotti.Finance.Repositories;
using LinqToExcel;
using Microsoft.SolverFoundation.Services;

namespace Bristotti.Finance.ExcelDataAccess
{
    public class YieldRepository : IYieldRepository
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
            var maxDate = new DateTime(date.Year + 2, 12, 31);

            return from x in Excel.Worksheet<CopomMeeting>("CopomMeeting")
                where x.Date >= date && x.Date <= maxDate
                select x;
        }

        public IEnumerable<DI1> GetDI1s(DateTime date)
        {
            Excel.AddMapping<DI1>(x => x.Spot, "Close");
            return from x in Excel.Worksheet<DI1>("di1")
                where x.MarketDate == date && x.TotalContracts > 0
                orderby x.MaturityDate
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
            var clock = Stopwatch.StartNew();
            
            var copomMeetings = GetCopomMeetings(date).ToArray();
            var di1s = GetDI1s(date).ToArray();
            var cdi = GetCDI(date);
            Console.WriteLine($"Load data={clock.ElapsedMilliseconds}");

            var yields = InitializeYield(date, cdi, copomMeetings, di1s).ToArray();

            var meetings = copomMeetings.Length;
            double bump = 0;
            var diArray = Enumerable.Range(0, yields.Length)
                .Where(i => yields[i].YieldType == YieldType.DI1)
                .Select(i => i).ToArray();
            var copomArray = Enumerable.Range(0, yields.Length)
                .Where(i => yields[i].YieldType == YieldType.COPOM)
                .Select(i => i).ToArray();

            var diPos = 0;
            var copomPos = 0;

            for (var i = 1; i < yields.Length; i++)
            {
                yields[i].Forward = yields[i - 1].Forward + yields[i - 1].Bump;
                yields[i].ForwardFactor = GetFactor(yields[i].Forward, yields[i].Term - yields[i - 1].Term);
                yields[i].SpotFactor = yields[i - 1].SpotFactor * yields[i].ForwardFactor;
                yields[i].Spot = GetInterest(yields[i].SpotFactor, yields[i].Term);

                if (yields[i].YieldType == YieldType.COPOM)
                {
                    meetings--;
                    copomPos++;
                }
                else
                {
                    diPos++;
                }

                var shouldBump = yields[i].YieldType == YieldType.COPOM || meetings == 0;

                if (shouldBump)
                {
                    var nextDI = diPos < diArray.Length ? diArray[diPos] : -1;
                    var nextNextDI = diPos + 1 < diArray.Length ? diArray[diPos + 1] : -1;
                    var nextCOPOM = copomPos < copomArray.Length ? copomArray[copomPos] : -1;

                    // não tem mais reunião do copom, portanto haverá bump no vencimento de DI1
                    if (meetings == 0)
                    {
                        if(i + 1 >= yields.Length)
                            continue;
                        var forwardFactor = GetFactor(yields[i + 1].SpotMtm, yields[i + 1].Term) / yields[i].SpotFactor;
                        var forward = GetInterest(forwardFactor, yields[i + 1].Term - yields[i].Term);
                        yields[i].Bump = forward - yields[i].Forward;
                    }
                    // os dois próximos vértices são de DI1
                    else if (nextDI - i == 1 && nextDI == nextNextDI - 1)
                    {
                        // calculamos o bump considerando apenas o vencimento de DI1 mais longo
                        var forwardFactor = GetFactor(yields[i + 2].SpotMtm, yields[i + 2].Term) / yields[i].SpotFactor;
                        var forward = GetInterest(forwardFactor, yields[i + 2].Term - yields[i].Term);
                        yields[i].Bump = forward - yields[i].Forward;
                    }
                    // o próximo vértice é um DI1 seguido de um COPOM
                    else if (nextCOPOM == nextDI + 1)
                    {
                        var forwardFactor = GetFactor(yields[i + 1].SpotMtm, yields[i + 1].Term) / yields[i].SpotFactor;
                        var forward = GetInterest(forwardFactor, yields[i + 1].Term - yields[i].Term);
                        yields[i].Bump = forward - yields[i].Forward;
                    }
                    // os dois próximos vértices são reuniôes do COPOM
                    else
                    {
                        FindForwards(
                            yields[i],
                            new[]
                            {
                                yields[i + 1], yields[i + 2]
                            },
                            yields[i + 2].SpotMtm);

                        yields[i].Bump = yields[i + 1].Forward - yields[i].Forward;
                        yields[i + 1].Bump = yields[i + 2].Forward - yields[i + 1].Forward;

                        i++;
                        meetings--;
                        yields[i].Forward = yields[i - 1].Forward + yields[i - 1].Bump;
                        yields[i].ForwardFactor = GetFactor(yields[i].Forward, yields[i].Term - yields[i - 1].Term);
                        yields[i].SpotFactor = yields[i - 1].SpotFactor * yields[i].ForwardFactor;
                        yields[i].Spot = GetInterest(yields[i].SpotFactor, yields[i].Term);
                    }
                }
                else
                {
                    yields[i].Bump = 0;
                }
            }

            Console.WriteLine($"Interpolate yield={clock.ElapsedMilliseconds}");
            return yields;
        }

        public HashSet<DateTime> GetHolidays()
        {
            return _holidays ?? (_holidays =
                       new HashSet<DateTime>(Excel.Worksheet<Holiday>("holiday").Select(x => x.Date).ToArray()));
        }

        private static double GetFactor(double r, double t)
        {
            return Math.Pow(1d + r / 100d, t / 252d);
        }

        private static double GetInterest(double f, double t)
        {
            return (Math.Pow(f, 252d / t) - 1) * 100;
        }

        private IEnumerable<Yield> InitializeYield(DateTime date, CDI cdi, CopomMeeting[] copomMeetings, DI1[] di1s)
        {
            var i_d = 0;
            var i_c = 0;

            yield return new Yield
            {
                Term = 0,
                Forward = cdi.TaxaCDI,
                SpotMtm = cdi.TaxaCDI,
                Spot = cdi.TaxaCDI,
                YieldType = YieldType.CDI,
                ForwardFactor = 1d,
                SpotFactor = 1d
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

        private static void FindForwards(Yield @short, Yield[] yields, double spotTarget)
        {
            var clock = Stopwatch.StartNew();
            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();
            Console.WriteLine($"Solver-Clean/Create model={clock.ElapsedMilliseconds}");

            var forwards = new Decision[yields.Length];
            for (var i = 0; i < forwards.Length; i++)
                model.AddDecision(forwards[i] = new Decision(Domain.RealNonnegative, null));

            Term fwd = @short.Forward;
            var spotFactor = Microsoft.SolverFoundation.Services.Model.Power(1d + @short.Spot / 100d, @short.Term / 252d);
            var diff = new Term[forwards.Length + 1];
            diff[0] = 0;

            for (var i = 0; i < forwards.Length; i++)
            {
                var termDelta = yields[i].Term - (i == 0 ? @short.Term : yields[i - 1].Term);
                var forwardFactor = Microsoft.SolverFoundation.Services.Model.Power(1d + forwards[i] / 100d, termDelta / 252d);
                spotFactor *= forwardFactor;
                diff[i + 1] = forwards[i] - (i == 0 ? fwd : forwards[i - 1]);
            }

            var spot = (Microsoft.SolverFoundation.Services.Model.Power(spotFactor, 252d / yields.Last().Term) - 1d) * 100d;

            var diff2 = new Term[diff.Length - 1];
            for (var i = 1; i < diff.Length; i++)
                diff2[i - 1] = diff[i] - diff[i - 1];

            var diff3 = new Term[diff2.Length - 1];
            for (var i = 1; i < diff2.Length; i++)
                diff3[i - 1] = diff2[i] - diff2[i - 1];

            var goal = Microsoft.SolverFoundation.Services.Model.Sum(Microsoft.SolverFoundation.Services.Model.Abs(spot - spotTarget), Microsoft.SolverFoundation.Services.Model.Abs(Microsoft.SolverFoundation.Services.Model.Sum(diff3)));


            model.AddGoal("erro", GoalKind.Minimize, goal);
            Console.WriteLine($"Solver-create goal={clock.ElapsedMilliseconds}");
            context.Solve();
            Console.WriteLine($"Solver-solve={clock.ElapsedMilliseconds}");

            for (var i = 0; i < forwards.Length; i++)
                yields[i].Forward = forwards[i].GetDouble();
        }
    }
}