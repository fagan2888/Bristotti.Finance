using System;

namespace ConsoleApp1
{
    partial class Program
    {
        public class Segment
        {
            // The name of the starting port.
            public string StartingPort { get; set; }

            // A unique identifier for the segment.
            public int Id { get; set; }

            // Segment distance in nautical miles.
            public double Distance { get; set; }

            // The earliest time (in hours) when the ship can depart from port.
            public double MinDepartDay { get; set; }

            // The earliest time (in days) when the ship can depart from port.
            public double MinDepartTime { get { return MinDepartDay * 24.0; } }

            // The latest time (in hours) when the ship can depart from port.
            public double MaxDepartDay { get; set; }

            // The latest time (in days) when the ship can depart from port.
            public double MaxDepartTime { get { return MaxDepartDay * 24.0; } }

            // The departure time.
            public double DepartTime { get; set; }

            // The departure day.
            public double DepartDay { get { return DepartTime / 24.0; } }

            // The average sailing speed (in knots).
            public double Knots { get; set; }

            // Time in port.
            public double WaitTime { get; set; }

            // Number of days in port.
            public double WaitDays { get { return WaitTime / 24.0; } }

            // Returns a string representation of the Segment.
            public override string ToString()
            {
                return String.Format("{0}   [{1}, {2}]   wait {5}   depart {3}   knots {4:f2}",
                    StartingPort.PadRight(15),
                    MinDepartDay.ToString().PadLeft(2),
                    MaxDepartDay.ToString().PadLeft(2),
                    DepartDay.ToString("f1").PadLeft(4),
                    Knots,
                    WaitDays.ToString("f1").PadLeft(4));
            }
        }
    }
}
