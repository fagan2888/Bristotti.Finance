using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Finance.Model;
using Microsoft.SolverFoundation.Services;

namespace Bristotti.Finance
{
    public class YieldCurveEngine
    {
        public static double GetFactor(double r, double t)
        {
            return Math.Pow(1d + r / 100d, t / 252d);
        }

        public static double GetInterest(double f, double t)
        {
            return (Math.Pow(f, 252d / t) - 1) * 100;
        }

        public IEnumerable<Yield> BuildYield(DateTime date, CopomMeeting[] copomMeetings, DI1[] di1s, CDI cdi, HashSet<DateTime> holidays)
        {
            var clock = Stopwatch.StartNew();

            var yields = InitializeYield(date, cdi, copomMeetings, di1s, holidays).ToArray();

            for (var i = 1; i < yields.Length; i++)
                yields[i].DeltaTerm = yields[i].Term - yields[i - 1].Term;

            yields = yields.Where(y => y.YieldType != YieldType.DI1 || (y.DeltaTerm > 1 && y.TotalTradesMtm > 1)).ToArray();

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
            yields[0].Forward = yields.First(y => y.YieldType == YieldType.DI1).SpotMtm;
            var lastBump = 0d;

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
                        if (i + 1 >= yields.Length)
                            continue;
                        var forwardFactor = GetFactor(yields[i + 1].SpotMtm, yields[i + 1].Term) / yields[i].SpotFactor;
                        var forward = GetInterest(forwardFactor, yields[i + 1].Term - yields[i].Term);
                        lastBump = yields[i].Bump = forward - yields[i].Forward;
                    }
                    // os dois próximos vértices são de DI1
                    else if (nextDI - i == 1 && nextDI == nextNextDI - 1)
                    {
                        // calculamos o bump considerando apenas o vencimento de DI1 mais longo
                        var forwardFactor = GetFactor(yields[i + 2].SpotMtm, yields[i + 2].Term) / yields[i].SpotFactor;
                        var forward = GetInterest(forwardFactor, yields[i + 2].Term - yields[i].Term);
                        lastBump = yields[i].Bump = forward - yields[i].Forward;
                    }
                    // o próximo vértice é um DI1 seguido de um COPOM
                    else if (nextCOPOM == nextDI + 1)
                    {
                        var forwardFactor = GetFactor(yields[i + 1].SpotMtm, yields[i + 1].Term) / yields[i].SpotFactor;
                        var forward = GetInterest(forwardFactor, yields[i + 1].Term - yields[i].Term);
                        lastBump = yields[i].Bump = forward - yields[i].Forward;
                    }
                    // os próximos n vértices são reuniôes do COPOM
                    else
                    {
                        var ps = new List<Yield>();
                        var j = i + 1;
                        while (j < yields.Length && yields[j].YieldType == YieldType.COPOM)
                            ps.Add(yields[j++]);

                        ps.Add(yields[j]);

                        FindForwards(
                            yields[i],
                            ps.ToArray(),
                            yields[j].SpotMtm,
                            lastBump);

                        yields[i].Bump = yields[i + 1].Forward - yields[i].Forward;

                        for (i = i + 1; i < j; i++)
                        {
                            lastBump = yields[i].Bump = yields[i + 1].Forward - yields[i].Forward;
                            yields[i].ForwardFactor = GetFactor(yields[i].Forward, yields[i].Term - yields[i - 1].Term);
                            yields[i].SpotFactor = yields[i - 1].SpotFactor * yields[i].ForwardFactor;
                            yields[i].Spot = GetInterest(yields[i].SpotFactor, yields[i].Term);
                            meetings--;
                            copomPos++;
                        }
                        i--;
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

        public IEnumerable<Yield> BuildYield2(DateTime date, CopomMeeting[] copomMeetings, DI1[] di1s, CDI cdi,
            HashSet<DateTime> holidays)
        {
            var clock = Stopwatch.StartNew();

            var yields = InitializeYield(date, cdi, copomMeetings, di1s, holidays).ToArray();

            for (var i = 1; i < yields.Length; i++)
                yields[i].DeltaTerm = yields[i].Term - yields[i - 1].Term;

            yields = yields.Where(y => y.YieldType != YieldType.DI1 || (y.DeltaTerm > 1 && y.TotalTradesMtm > 1)).ToArray();

            var meetings = copomMeetings.Length;
            var diArray = Enumerable.Range(0, yields.Length)
                .Where(i => yields[i].YieldType == YieldType.DI1)
                .Select(i => i).ToArray();

            var diPos = 0;
            var lastCOPOM = -1;

            for (var i = 1; i < yields.Length; i++)
            {
                var nextDI = diPos < diArray.Length ? diArray[diPos] : -1;

                if (yields[i].YieldType == YieldType.COPOM)
                {
                    yields[i].IsSameForward = lastCOPOM != i - 1;
                    lastCOPOM = i;
                    meetings--;
                }
                else
                {
                    if(nextDI == i + 1 && meetings > 0)
                        yields[i].IsSameForward = true;
                    else
                        yields[i].IsSameForward = false;
                    diPos++;
                }
            }

            Optimize(yields);

            Console.WriteLine($"Interpolate yield={clock.ElapsedMilliseconds}");
            return yields;
        }

        private IEnumerable<Yield> InitializeYield(DateTime date, CDI cdi, CopomMeeting[] copomMeetings, DI1[] di1s, HashSet<DateTime> holidays)
        {
            var i_d = 0;
            var i_c = 0;

            yield return new Yield
            {
                Term = 0,
                Maturity = date,
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
                        Term = GetNetworkDays(date, copomMeetings[i_c].Date, holidays),
                        Maturity = copomMeetings[i_c].Date,
                        YieldType = YieldType.COPOM
                    };
                    i_c++;
                }
                else
                {
                    yield return new Yield
                    {
                        Term = GetNetworkDays(date, di1s[i_d].MaturityDate, holidays),
                        Maturity = di1s[i_d].MaturityDate,
                        YieldType = YieldType.DI1,
                        SpotMtm = di1s[i_d].Spot,
                        TotalTradesMtm = di1s[i_d].TotalTrades
                    };
                    i_d++;
                }
            }

            for (; i_c < copomMeetings.Length; i_c++)
                yield return new Yield
                {
                    Term = GetNetworkDays(date, copomMeetings[i_c].Date, holidays),
                    Maturity = copomMeetings[i_c].Date,
                    YieldType = YieldType.COPOM
                };

            for (; i_d < di1s.Length; i_d++)
                yield return new Yield
                {
                    Term = GetNetworkDays(date, di1s[i_d].MaturityDate, holidays),
                    Maturity = di1s[i_d].MaturityDate,
                    YieldType = YieldType.DI1,
                    SpotMtm = di1s[i_d].Spot,
                    TotalTradesMtm = di1s[i_d].TotalTrades
                };
        }

        private static void FindForwards(Yield @short, Yield[] yields, double spotTarget, double initialDiff = 0)
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
            diff[0] = initialDiff;

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


        private static void Optimize(Yield[] yields)
        {
            var clock = Stopwatch.StartNew();

            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();
            Console.WriteLine($"Solver-Clean/Create model={clock.ElapsedMilliseconds}");

            var initialValue = yields[0].SpotMtm;

            var forwards = new Decision[yields.Length];
            for (var i = 1; i < forwards.Length; i++)
            {
                if (yields[i].IsSameForward)
                    forwards[i] = forwards[i - 1];
                else
                {
                    initialValue = yields[i].YieldType == YieldType.DI1 ? yields[i].SpotMtm : initialValue;
                    forwards[i] = new Decision(Domain.RealNonnegative, null);
                    forwards[i].SetInitialValue(initialValue);
                    model.AddDecision(forwards[i]);
                }
            }

            var spotFactor = (Term)1d;
            var erro = (Term) 0d;
            var diff = new Term[forwards.Length - 1];

            for (var i = 1; i < forwards.Length; i++)
            {
                var termDelta = yields[i].Term - yields[i - 1].Term;
                var forwardFactor = Microsoft.SolverFoundation.Services.Model.Power(1d + forwards[i] / 100d, termDelta / 252d);
                spotFactor *= forwardFactor;
                var spot = (Microsoft.SolverFoundation.Services.Model.Power(spotFactor, 252d / yields[i].Term) - 1d) * 100d;
                if(yields[i].YieldType == YieldType.DI1)
                    erro += Microsoft.SolverFoundation.Services.Model.Abs(yields[i].SpotMtm - spot);

                //if (i == 1)
                //    diff[i - 1] = forwards[i] - (i == 1 ? (Term) 0 : forwards[i - 1]);
            }

            //var diff2 = new Term[diff.Length - 1];
            //for (var i = 1; i < diff.Length; i++)
            //    diff2[i - 1] = diff[i] - diff[i - 1];

            //var diff3 = new Term[diff2.Length - 1];
            //for (var i = 1; i < diff2.Length; i++)
            //    diff3[i - 1] =  diff2[i] - diff2[i - 1];

            model.AddGoal("erro", GoalKind.Minimize, erro);
            Console.WriteLine($"Solver-create goal={clock.ElapsedMilliseconds}");
            context.Solve();
            clock.Stop();
            Console.WriteLine($"Solver-solve={clock.ElapsedMilliseconds}");

            var spotfactor = 1d;
            for (var i = 1; i < forwards.Length; i++)
            {
                yields[i].Forward = forwards[i].GetDouble();
                spotfactor *= Math.Pow(1d + yields[i].Forward / 100, (yields[i].Term - yields[i - 1].Term) / 252);

                yields[i].SpotFactor = spotfactor;
                yields[i].Spot = (Math.Pow(spotfactor, 252d / yields[i].Term) - 1d) * 100d;
            }
        }

        public int GetNetworkDays(DateTime from, DateTime to, HashSet<DateTime> holidays)
        {
            if (from > to)
                throw new InvalidOperationException();

            if (from == to)
                return 0;

            int networkDays = 0;
            while (from < to)
            {
                if (!holidays.Contains(from))
                    networkDays++;

                if (from.DayOfWeek == DayOfWeek.Friday)
                    from = from.AddDays(3);
                else if (from.DayOfWeek == DayOfWeek.Saturday)
                    from = from.AddDays(2);
                else
                    from = from.AddDays(1);
            }
            return networkDays;
        }
    }
}
