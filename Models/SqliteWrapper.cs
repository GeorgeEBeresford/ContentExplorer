using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace ContentExplorer.Models
{
    public class SqliteWrapper : IDisposable
    {
        public SqliteWrapper(string connectionString)
        {
            try
            {
                SqlConnection = GenerateConnection(connectionString);
            }
            catch (ArgumentException)
            {
                // If we've passed the name of a connection string instead, try to get the value instead
                connectionString = GetConnectionStringFromName(connectionString);
                SqlConnection = GenerateConnection(connectionString);
            }
        }

        private string GetConnectionStringFromName(string connectionStringName)
        {
            ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            string connectionString =connectionStringSettings.ToString();

            return connectionString;
        }


        private SqliteConnection SqlConnection { get; }

        public static SqliteParameter GenerateParameter(string name, object value)
        {
            SqliteParameter parameter = new SqliteParameter(name, value);

            return parameter;
        }

        public void Dispose()
        {
            SqlConnection?.Dispose();
        }

        public bool ExecuteNonQuery(string query)
        {
            return ExecuteNonQuery(query, parameters: null);
        }

        public bool ExecuteNonQuery(string query, SqliteParameter parameter)
        {
            return ExecuteNonQuery(query, new SqliteParameter[] {parameter});
        }

        public bool ExecuteNonQuery(string query, IEnumerable<SqliteParameter> parameters)
        {
            SqliteCommand command = GenerateCommand(query, parameters);
            int updatedRecordCount = command.ExecuteNonQuery();

            bool isSuccess = updatedRecordCount != 0;

            return isSuccess;
        }

        public ICollection<IDictionary<string, object>> GetDataRows(string query)
        {
            return GetDataRows(query, new SqliteParameter[0]);
        }

        public ICollection<IDictionary<string, object>> GetDataRows(string query, SqliteParameter parameter)
        {
            return GetDataRows(query, new SqliteParameter[] {parameter});
        }

        public ICollection<IDictionary<string, object>> GetDataRows(string query, IEnumerable<SqliteParameter> parameters)
        {
            SqliteCommand command = GenerateCommand(query, parameters);
            SqliteDataReader dataReader = command.ExecuteReader();

            ICollection<IDictionary<string, object>> dataRows = GetDataRows(dataReader);
            return dataRows;
        }

        public IDictionary<string, object> GetDataRow(string query)
        {
            return GetDataRow(query, new SqliteParameter[0]);
        }

        public IDictionary<string, object> GetDataRow(string query, SqliteParameter parameter)
        {
            return GetDataRow(query, new SqliteParameter[] {parameter});
        }

        public IDictionary<string, object> GetDataRow(string query, IEnumerable<SqliteParameter> parameters)
        {
            SqliteCommand command = GenerateCommand(query, parameters);
            SqliteDataReader dataReader = command.ExecuteReader(CommandBehavior.SingleRow);

            IDictionary<string, object> dataRow = GetDataRows(dataReader).FirstOrDefault();
            return dataRow;
        }

        public object GetScalar(string query)
        {
            return GetScalar(query, new SqliteParameter[0]);
        }

        public object GetScalar(string query, SqliteParameter parameter)
        {
            return GetScalar(query, new SqliteParameter[] {parameter});
        }

        public object GetScalar(string query, IEnumerable<SqliteParameter> parameters)
        {
            SqliteCommand command = GenerateCommand(query, parameters);

            object scalar = command.ExecuteScalar();

            return scalar;
        }

        private static SqliteConnection GenerateConnection(string connectionString)
        {
            SqliteConnection connection = new SqliteConnection(connectionString);

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
            }

            return connection;
        }

        private ICollection<IDictionary<string, object>> GetDataRows(SqliteDataReader dataReader)
        {
            DataTable table = dataReader.GetSchemaTable();
            ICollection<string> columnNames = dataReader.GetColumnSchema()
                .Select(column => column.ColumnName)
                .ToArray();

            ICollection<IDictionary<string, object>> dataRows = new List<IDictionary<string, object>>();
            while (dataReader.Read())
            {
                Dictionary<string, object> dataRow = columnNames
                    .Select(columnName =>
                        new KeyValuePair<string, object>(columnName, dataReader[columnName])
                    )
                    .ToDictionary(keyPair => keyPair.Key, keyPair => keyPair.Value);

                dataRows.Add(dataRow);
            }

            return dataRows;
        }

        private SqliteCommand GenerateCommand(string query, IEnumerable<SqliteParameter> parameters)
        {
            SqliteCommand command = SqlConnection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = 120;

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            return command;
        }
    }
}