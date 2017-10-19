using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Bristotti.Finance.ExcelDataAccess;
using Bristotti.Finance.Model;
using LinqToExcel;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;
using Remotion.Utilities;

namespace ConsoleApp1
{
    internal partial class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
           
            ReadExcel();

            //var repo = new YieldRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.xlsx"));
            var repo = new YieldRepository("sample.xlsx");
            while (Console.ReadLine() != "q")
            {
                var clock = Stopwatch.StartNew();
                var yields = repo.BuildYield(new DateTime(2015, 4, 2)).ToArray();

                Console.WriteLine($"ElapsedMilliseconds={clock.ElapsedMilliseconds}");
                //MinimizeSimple();

                var form = new Form();
                var grid = new DataGridView();
                form.Controls.Add(grid);
                grid.Dock = DockStyle.Fill;
                grid.AutoGenerateColumns = true;
                grid.DataSource = yields;
                grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
                form.ShowDialog();
            }
        }

        private static void ReadExcel()
        {
            var repo = new YieldRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.xlsx"));

            
            var date = new DateTime(2015, 4, 2);

            var meetings = repo.GetCopomMeetings(date).ToArray();
            Debug.Assert(meetings.Length == 22);


            var di1s = repo.GetDI1s(date).ToArray();
            Debug.Assert(di1s.Length == 37);


            var from = date;
            var to = date.AddDays(1);
            int days = repo.GetNetworkDays(from, to);
            Debug.Assert(days == 1);


            var yields = repo.BuildYield(date).ToArray();
            Debug.Assert(yields.Length == meetings.Length + di1s.Length + 1);
        }

        private static void MinimizeSample()
        {
            Segment[] segmentData =
            {
                new Segment
                {
                    Id = 0,
                    Distance = 0,
                    MinDepartDay = 0,
                    MaxDepartDay = 0,
                    StartingPort = "Vancouver"
                },
                new Segment
                {
                    Id = 1,
                    Distance = 510,
                    MinDepartDay = 1,
                    MaxDepartDay = 4,
                    StartingPort = "Seattle"
                },
                new Segment
                {
                    Id = 2,
                    Distance = 2699,
                    MinDepartDay = 50,
                    MaxDepartDay = 65,
                    StartingPort = "Busan"
                },
                new Segment
                {
                    Id = 3,
                    Distance = 838,
                    MinDepartDay = 70,
                    MaxDepartDay = 75,
                    StartingPort = "Kaohsiung"
                },
                new Segment
                {
                    Id = 4,
                    Distance = 3625,
                    MinDepartDay = 74,
                    MaxDepartDay = 80,
                    StartingPort = "Hong Kong"
                }
            };

            var context = SolverContext.GetContext();
            var model = context.CreateModel();

            var segments = new Set(Domain.Integer, "segments");

            var distance = new Parameter(Domain.RealNonnegative, "distance", segments);
            distance.SetBinding(segmentData, "Distance", "Id");

            var early = new Parameter(Domain.RealNonnegative, "early", segments);
            early.SetBinding(segmentData, "MinDepartTime", "Id");

            var late = new Parameter(Domain.RealNonnegative, "late", segments);
            late.SetBinding(segmentData, "MaxDepartTime", "Id");
            model.AddParameters(distance, early, late);

            var speed = new Decision(Domain.RealRange(14, 20), "speed", segments);
            speed.SetBinding(segmentData, "Knots", "Id");

            var time = new Decision(Domain.RealRange(0, 100 * 24), "time", segments);
            time.SetBinding(segmentData, "DepartTime", "Id");

            var wait = new Decision(Domain.RealRange(0, 100 * 24), "wait", segments);
            wait.SetBinding(segmentData, "WaitTime", "Id");
            model.AddDecisions(speed, time, wait);

            model.AddConstraint("bounds", Model.ForEach(segments, s => early[s] <= time[s] <= late[s]));

            // The departure time for segment s is the sum of departure time for the previous segment,
            // the sailing time, and time in port.
            model.AddConstraint("times", Model.ForEachWhere(segments,
                s => time[s - 1] + distance[s - 1] / speed[s - 1] + wait[s] == time[s],
                s => s > 0));

            model.AddConstraint("wait_0", wait[0] == 0);

            var fuel = model.AddGoal("fuel", GoalKind.Minimize, Model.Sum(Model.ForEach(segments,
                s => distance[s] * (0.0036 * Model.Power(speed[s], 2) - 0.1015 * speed[s] + 0.8848)
                     + 0.01 * wait[s])));

            context.Solve();
            context.PropagateDecisions();

            Console.WriteLine($"Fuel consumption: {fuel.ToDouble():f2}");
            Console.WriteLine();
            Console.WriteLine("Schedule:");
            Console.WriteLine(new string('-', segmentData[0].ToString().Length));
            foreach (var seg in segmentData)
                Console.WriteLine(seg);

            Console.ReadLine();
        }

        private static void MinimizeSimple()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();

            var @short = new Yield
            {
                Term = 325,
                Forward = 13.88,
                Spot = 13.390356079286300000
            };

            var yields = new[]
            {
                new Yield
                {
                    Term = 355
                }, 
                new Yield
                {
                    Term = 377
                }
            };

            FindForwards(
                @short,
                yields,
                13.32);

            Console.WriteLine($"Elapsed={clock.ElapsedMilliseconds} Forwards={string.Join(";", yields.Select(y => $"t={y.Term};fwd={y.Forward}"))}");
        }

        private static void FindForwards(Yield @short, Yield[] yields, double spotTarget)
        {
            var context = SolverContext.GetContext();
            context.ClearModel();
            var model = context.CreateModel();

            var forwards = new Decision[yields.Length];
            for (var i = 0; i < forwards.Length; i++)
                model.AddDecision(forwards[i] = new Decision(Domain.RealNonnegative, null));

            Term fwd = @short.Forward;
            var spotFactor = Model.Power(1d + @short.Spot / 100d, @short.Term / 252d);
            var diff = new Term[forwards.Length + 1];
            diff[0] = 0;

            for (var i = 0; i < forwards.Length; i++)
            {
                var termDelta = yields[i].Term - (i == 0 ? @short.Term : yields[i - 1].Term);
                var forwardFactor = Model.Power(1d + forwards[i] / 100d, termDelta / 252d);
                spotFactor *= forwardFactor;
                diff[i + 1] = forwards[i] - (i == 0 ? fwd : forwards[i - 1]);
            }

            var spot = (Model.Power(spotFactor, 252d / yields.Last().Term)-1d)*100d;

            var diff2 = new Term[diff.Length - 1];
            for (var i = 1; i < diff.Length; i++)
                diff2[i - 1] = diff[i] - diff[i - 1];

            var diff3 = new Term[diff2.Length - 1];
            for (var i = 1; i < diff2.Length; i++)
                diff3[i - 1] = diff2[i] - diff2[i - 1];

            var goal = Model.Sum(Model.Abs(spot - spotTarget), Model.Abs(Model.Sum(diff3)));

            model.AddGoal("erro", GoalKind.Minimize, goal);

            context.Solve();

            for (var i = 0; i < forwards.Length; i++)
                yields[i].Forward = forwards[i].GetDouble();
        }
    }
}