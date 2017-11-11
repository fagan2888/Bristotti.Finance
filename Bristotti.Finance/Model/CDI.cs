using System;

namespace Bristotti.Finance.Model
{
    public class CDI
    {
        public DateTime Date { get; set; }
        public double Operacoes { get; set; }
        public double TaxaCDI { get; set; }
        public double TaxaSELIC { get; set; }
    }
}