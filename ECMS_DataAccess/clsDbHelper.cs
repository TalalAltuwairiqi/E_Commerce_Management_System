using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    // Small helper that opens the connection, runs a query and closes everything.
    // The other Data Access classes use it so they only contain SQL and parameters.
    internal static class clsDbHelper
    {
        // SELECT -> returns a table of rows
        public static DataTable GetTable(string sql, Action<SqlCommand>? setup = null,
                                         CommandType commandType = CommandType.Text)
        {
            DataTable table = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandType = commandType;
                setup?.Invoke(command);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    table.Load(reader);
                }
            }

            return table;
        }

        // Returns the first column of the first row (for COUNT, SCOPE_IDENTITY, ...)
        public static object? ExecuteScalar(string sql, Action<SqlCommand>? setup = null)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                setup?.Invoke(command);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        // INSERT / UPDATE / DELETE -> returns the number of affected rows
        public static int ExecuteNonQuery(string sql, Action<SqlCommand>? setup = null)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                setup?.Invoke(command);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }
    }
}