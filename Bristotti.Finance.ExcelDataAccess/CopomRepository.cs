using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Common.Data;
using Bristotti.Finance.Model;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Bristotti.Finance.ExcelDataAccess
{
    public class CopomRepository : ExcelRepository, ICopomRepository
    {
        public CopomRepository(string file) : base(file)
        {
        }

        public IList<CopomMeeting> GetMeetings()
        {
            return GetEntity<CopomMeeting>("CopomMeeting");
        }
    }
}
