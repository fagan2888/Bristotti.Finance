using System.Collections.Generic;
using Bristotti.Finance.Model;

namespace Bristotti.Finance.ExcelDataAccess
{
    public class HolidayRepository : ExcelRepository, IHolidayRepository
    {
        public HolidayRepository(string file) : base(file)
        {
        }
     
        public IList<Holiday> GetFeriados(string feriado)
        {
            return GetEntity<Holiday>("Holiday" + feriado);
        }
    }
}