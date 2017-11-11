using System;

namespace Bristotti.Finance.Model
{
    public class DI1
    {
        public DateTime MarketDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public double Spot { get; set; }
        public double TotalContracts { get; set; }
        public double TotalTrades { get; set; }
    }
}