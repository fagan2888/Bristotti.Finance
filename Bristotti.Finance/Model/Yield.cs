using System;

namespace Bristotti.Finance.Model
{
    public class Yield
    {
        public int Id { get; set; }
        public DateTime Maturity { get; set; }
        public double Term { get; set; }
        public double DeltaTerm { get; set; }
        public double Forward { get; set; }
        public double Spot { get; set; }
        public YieldType YieldType { get; set; }
        public double SpotMtm { get; set; }
        public double SpotMtmFactor { get; set; }
        public double SpotFactor { get; set; }
        public double ForwardFactor { get; set; }
        public double Bump { get; set; }
        public double Erro => YieldType == YieldType.DI1 ? Spot - SpotMtm : 0;
        public bool IsSameForward { get; set; }
        public double TotalTradesMtm { get; set; }
    }

    public enum YieldType
    {
        COPOM, DI1, CDI
    }
}
