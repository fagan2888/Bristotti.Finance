using System;
using System.Collections.Generic;
using Bristotti.Finance.Model;

namespace Bristotti.Finance.ExcelDataAccess
{
    public class DI1Repository : ExcelRepository, IDI1Repository
    {
        public DI1Repository(string file) : base(file)
        {
        }

        public IList<DI1> GetByMarketDate(DateTime date)
        {
            return GetEntity<DI1>("DI1", $"WHERE [MARKETDATE] = #{date:d}#");
        }
    }
}