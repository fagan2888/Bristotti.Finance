namespace ConsoleApp1
{
    partial class Program
    {
        public class Yield
        {
            public int Id { get; set; }
            public bool SameFwd { get; set; }
            public double Term { get; set; }
            public double? TermDelta { get; set; }
            public double? rm { get; set; }
            public double? FwdInput { get; set; }

            public double? Fwd { get; set; }
            public double FwdFactor { get; set; }
            public double r { get; set; }
            public double rFactor { get; set; }
            public double Erro { get; set; }
        }
    }
}
