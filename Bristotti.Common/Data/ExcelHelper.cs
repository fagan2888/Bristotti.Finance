using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bristotti.Common.Data
{
    public static class ExcelHelper
    {
        public static DataTable GetTable(string file, string sheetName)
        {
            var table = new DataTable(sheetName);

            var query = $"SELECT * FROM [{sheetName.ToUpper()}$]";

            using (var connection = OpenConnection(file))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                var reader = command.ExecuteReader();
                table.Load(reader);
            }
            return table;
        }

        public static IList<T> List<T>(string file, string sheetName) where T : new()
        {
            return GetTable(file, sheetName)
                .Transform<T>()
                .ToList();
        }

        public static IList<T> List<T>(string file, string sheetName, string whereClause) where T : new()
        {
            var table = new DataTable(sheetName);

            var query = $"SELECT * FROM [{sheetName.ToUpper()}$] {whereClause}";

            using (var connection = OpenConnection(file))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                var reader = command.ExecuteReader();
                table.Load(reader);
            }

            return table.Transform<T>().ToList();
        }

        public static IDbConnection OpenConnection(string file)
        {
            var connectionString = $"Driver={{Microsoft Excel Driver (*.xls, *.xlsx, *.xlsm, *.xlsb)}};DBQ={file};";
            var connection = new OdbcConnection(connectionString);

            connection.Open();
            return connection;
        }
    }
}
