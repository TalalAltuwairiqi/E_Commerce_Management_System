using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    public static class clsCustomerData
    {
        public static DataTable GetAllCustomers(string search, bool activeOnly)
        {
            string sql = @"SELECT c.CustomerID, c.PersonID,
                                  p.FirstName + N' ' + p.LastName AS FullName,
                                  p.Phone, p.Email, p.Address, c.CreatedAt, c.IsActive
                           FROM dbo.Customers c
                           JOIN dbo.People p ON p.PersonID = c.PersonID
                           WHERE (@ActiveOnly = 0 OR c.IsActive = 1)
                             AND (p.FirstName LIKE @Pattern
                                  OR p.LastName LIKE @Pattern
                                  OR (p.FirstName + N' ' + p.LastName) LIKE @Pattern
                                  OR p.Phone LIKE @Pattern
                                  OR p.Email LIKE @Pattern)
                           ORDER BY p.FirstName, p.LastName;";

            return clsDbHelper.GetTable(sql, cmd =>
            {
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%";
                cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly;
            });
        }

        public static DataTable GetCustomerByID(int customerID)
        {
            string sql = @"SELECT CustomerID, PersonID, CreatedAt, IsActive
                           FROM dbo.Customers
                           WHERE CustomerID = @CustomerID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@CustomerID", SqlDbType.Int).Value = customerID);
        }

        // Returns the new CustomerID (0 if it failed)
        public static int AddNewCustomer(int personID, bool isActive)
        {
            string sql = @"INSERT INTO dbo.Customers (PersonID, IsActive)
                           VALUES (@PersonID, @IsActive);
                           SELECT CAST(SCOPE_IDENTITY() AS int);";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
            });

            if (result != null && int.TryParse(result.ToString(), out int newID))
                return newID;

            return 0;
        }

        public static bool UpdateCustomer(int customerID, bool isActive)
        {
            string sql = "UPDATE dbo.Customers SET IsActive = @IsActive WHERE CustomerID = @CustomerID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@CustomerID", SqlDbType.Int).Value = customerID;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
            });

            return rows > 0;
        }

        public static bool IsPersonAlreadyCustomer(int personID)
        {
            string sql = "SELECT COUNT(1) FROM dbo.Customers WHERE PersonID = @PersonID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID);

            return Convert.ToInt32(result) > 0;
        }

        // People who are not customers yet (used by the "Add Customer" form)
        public static DataTable GetPeopleWithoutCustomer()
        {
            string sql = @"SELECT p.PersonID,
                                  p.FirstName + N' ' + p.LastName + N'  (' + p.Phone + N')' AS DisplayName
                           FROM dbo.People p
                           WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.PersonID = p.PersonID)
                           ORDER BY p.FirstName, p.LastName;";

            return clsDbHelper.GetTable(sql);
        }

        // Uses the stored procedure created in 04_procedures.sql
        public static DataTable GetCustomerOrders(int customerID)
        {
            return clsDbHelper.GetTable("dbo.sp_GetCustomerOrders", cmd =>
                cmd.Parameters.Add("@CustomerID", SqlDbType.Int).Value = customerID,
                CommandType.StoredProcedure);
        }
    }
}