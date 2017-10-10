namespace ConsoleApp1
{
    public class Yield
    {
        public int Id { get; set; }
        public double Term { get; set; }
        public double Forward { get; set; }
        public double Spot { get; set; }
        public YieldType YieldType { get; set; }
        public double SpotMtm { get; set; }
    }

    public enum YieldType
    {
        COPOM, DI1, CDI
    }
}
