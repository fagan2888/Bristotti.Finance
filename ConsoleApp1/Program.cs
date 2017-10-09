using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Common.Data;
using Bristotti.Finance.ExcelDataAccess;
using Bristotti.Finance.Model;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;

namespace ConsoleApp1
{
    partial class Program
    {
        static void Main(string[] args)
        {
            //MinimizeSample();
            MinimizeSimple();
        }

        static void MinimizeSample()
        {
            Segment[] segmentData = {
                new Segment { Id = 0, Distance = 0, MinDepartDay = 0, MaxDepartDay = 0,
                    StartingPort = "Vancouver" },
                new Segment { Id = 1, Distance = 510, MinDepartDay = 1, MaxDepartDay = 4,
                    StartingPort = "Seattle" },
                new Segment { Id = 2, Distance = 2699, MinDepartDay = 50, MaxDepartDay = 65,
                    StartingPort = "Busan" },
                new Segment { Id = 3, Distance = 838, MinDepartDay = 70, MaxDepartDay = 75,
                    StartingPort = "Kaohsiung" },
                new Segment { Id = 4, Distance = 3625, MinDepartDay = 74, MaxDepartDay = 80,
                    StartingPort = "Hong Kong" }
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
                s => (s > 0)));

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

        static void MinimizeSimple()
        {
            var yieldData = new[]
            {
                new SimpleYield
                {
                    Id = 0,
                    Forward = 1.015051026
                },
                new SimpleYield
                {
                    Id = 1,
                    Forward = 1.010976474
                }
            };

            var context = SolverContext.GetContext();
            context.ClearModel();

            var model = context.CreateModel();
            var fwds = new Decision[yieldData.Length];
            for (var i = 0; i < fwds.Length; i++)
                model.AddDecision(fwds[i] = new Decision(Domain.RealNonnegative, null));

            Term fwd = 1.0253168688028800000;
            Term fwdD0 = 1.006728464462830000;
            Term product = 1f;
            var diff = new Term[fwds.Length];
            for (var i = 0; i < fwds.Length; i++)
            {
                var prev = i == 0 ? fwdD0 : fwds[i - 1];
                product *= fwds[i];
                diff[i] = fwds[i] - prev;
            }

            var diff2 = new Term[fwds.Length - 1];
            for (var i = 1; i < fwds.Length; i++)
            {
                diff2[i - 1] = diff[i] - diff[i - 1];
            }

            //var goal = Model.Abs((fwd - product) * 10000 + Model.Abs(Model.Sum(diff2)) * 10000);
            var goal = Model.Abs((fwd - product) * 10000);

            model.AddGoal("erro", GoalKind.Minimize, goal);
            
            var solution = context.Solve();
            
            Console.WriteLine("e = {0}", solution.Goals.First().ToDouble());
            for (var i = 0; i < fwds.Length; i++) Console.WriteLine("fwd[{0}] = {1}", i, fwds[i].GetDouble());

            Console.ReadLine();
        }

        private static void Minimize(Yield[] yields)
        {
            var solver = new CompactQuasiNewtonSolver();
            var model = SolverContext.GetContext().CreateModel();

            var yieldSet = new Set(Domain.Integer, "yields");
            var sameFwdParameter = new Parameter(Domain.Boolean, "sameFwd", yieldSet);
            sameFwdParameter.SetBinding(yields, "SameFwd", "Id");
            var termParameter = new Parameter(Domain.RealNonnegative, "term", yieldSet);
            termParameter.SetBinding(yields, "Term", "Id");
            var termDeltaParameter = new Parameter(Domain.RealNonnegative, "termDelta", yieldSet);
            termDeltaParameter.SetBinding(yields, "TermDelta", "Id");
            var rmParameter = new Parameter(Domain.RealNonnegative, "rm", yieldSet);
            rmParameter.SetBinding(yields, "TermDelta", "Id");

            model.AddParameters(sameFwdParameter, termParameter, termDeltaParameter, rmParameter);

            var fwdDecision = new Decision(Domain.RealNonnegative, "FwdInput", yieldSet);
            fwdDecision.SetBinding(yields, "FwdInput", "Id");
            model.AddDecision(fwdDecision);

            model.AddGoal("error", GoalKind.Minimize, Model.Sum(Model.ForEach(yieldSet, t =>
            {
                var fwd = Model.If(sameFwdParameter[t], fwdDecision[t], fwdDecision[t]);

                var fwdFactor = Model.Power(1 + fwd / 100, termDeltaParameter / 252);



                return fwdFactor;
            })));
        }
    }
}
