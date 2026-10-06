using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    // Data Access Layer: ADO.NET only. No business rules and no forms here.
    public static class clsDatabaseData
    {
        // Tries to open a connection and tells the caller what happened.
        public static bool TestConnection(out string message)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
                {
                    connection.Open();
                    message = "Connected to database '" + connection.Database + "' on server '" + connection.DataSource + "'";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        // Returns the number of rows in each table (same idea as Section A in 05_test_queries.sql).
        public static DataTable GetTableRowCounts()
        {
            DataTable dt = new DataTable();

            string query = @"SELECT 'People' AS TableName, COUNT(*) AS TotalRows FROM dbo.People
                             UNION ALL SELECT 'Users',          COUNT(*) FROM dbo.Users
                             UNION ALL SELECT 'Customers',      COUNT(*) FROM dbo.Customers
                             UNION ALL SELECT 'Categories',     COUNT(*) FROM dbo.Categories
                             UNION ALL SELECT 'Products',       COUNT(*) FROM dbo.Products
                             UNION ALL SELECT 'Orders',         COUNT(*) FROM dbo.Orders
                             UNION ALL SELECT 'OrderItems',     COUNT(*) FROM dbo.OrderItems
                             UNION ALL SELECT 'Payments',       COUNT(*) FROM dbo.Payments
                             UNION ALL SELECT 'StockMovements', COUNT(*) FROM dbo.StockMovements;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }
    }
}