using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bristotti.Common.Data
{
    public static class DataTableHelper
    {
        public static IEnumerable<T> Transform<T>(this DataTable table) where T : new()
        {
            var properties = TypeDescriptor.GetProperties(typeof(T));

            var matches =
            (from c in table.Columns.Cast<DataColumn>()
                let property = properties.Find(c.ColumnName, true)
                select new
                {
                    Column = c,
                    Property = property,
                }).ToArray();


            foreach (var row in table.Rows.Cast<DataRow>())
            {
                var item = new T();

                foreach (var match in matches)
                {
                    var value = row[match.Column];
                    if (value == null || value == DBNull.Value)
                        continue;

                    match.Property.SetValue(item, value);
                }

                yield return item;
            }
        }
    }
}
