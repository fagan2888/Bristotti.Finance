using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Common.Data;
using Bristotti.Finance.Model;

namespace Bristotti.Finance.ExcelDataAccess
{
    public abstract class ExcelRepository : IDisposable
    {
        private readonly string _file;

        protected ExcelRepository(string file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        protected IList<T> GetEntity<T>(string sheetName) where T : new()
        {
            return ExcelHelper.List<T>(_file, sheetName);
        }

        protected IList<T> GetEntity<T>(string sheetName, string whereClause) where T : new()
        {
            return ExcelHelper.List<T>(_file, sheetName, whereClause);
        }

        protected virtual void Dispose(bool disposing)
        {
            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
