using System;
using System.Collections.Generic;
using Bristotti.Finance.Model;

namespace Bristotti.Finance
{
    public interface IHolidayRepository : IDisposable
    {
        IList<Holiday> GetFeriados(string feriado);
    }
}