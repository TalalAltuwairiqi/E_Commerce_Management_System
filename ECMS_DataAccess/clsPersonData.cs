using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    public static class clsPersonData
    {
        public static DataTable GetAllPeople(string search)
        {
            string sql = @"SELECT PersonID, FirstName, LastName, Phone, Email, Address, DateOfBirth
                           FROM dbo.People
                           WHERE FirstName LIKE @Pattern
                              OR LastName  LIKE @Pattern
                              OR (FirstName + N' ' + LastName) LIKE @Pattern
                              OR Phone LIKE @Pattern
                              OR Email LIKE @Pattern
                           ORDER BY FirstName, LastName;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%");
        }

        public static DataTable GetPersonByID(int personID)
        {
            string sql = @"SELECT PersonID, FirstName, LastName, Phone, Email, Address, DateOfBirth
                           FROM dbo.People
                           WHERE PersonID = @PersonID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID);
        }

        // Returns the new PersonID (0 if it failed)
        public static int AddNewPerson(string firstName, string lastName, string phone,
                                       string? email, string? address, DateTime? dateOfBirth)
        {
            string sql = @"INSERT INTO dbo.People (FirstName, LastName, Phone, Email, Address, DateOfBirth)
                           VALUES (@FirstName, @LastName, @Phone, @Email, @Address, @DateOfBirth);
                           SELECT CAST(SCOPE_IDENTITY() AS int);";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                AddPersonParameters(cmd, firstName, lastName, phone, email, address, dateOfBirth));

            if (result != null && int.TryParse(result.ToString(), out int newID))
                return newID;

            return 0;
        }

        public static bool UpdatePerson(int personID, string firstName, string lastName, string phone,
                                        string? email, string? address, DateTime? dateOfBirth)
        {
            string sql = @"UPDATE dbo.People
                           SET FirstName = @FirstName, LastName = @LastName, Phone = @Phone,
                               Email = @Email, Address = @Address, DateOfBirth = @DateOfBirth
                           WHERE PersonID = @PersonID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID;
                AddPersonParameters(cmd, firstName, lastName, phone, email, address, dateOfBirth);
            });

            return rows > 0;
        }

        public static bool DeletePerson(int personID)
        {
            string sql = "DELETE FROM dbo.People WHERE PersonID = @PersonID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID);

            return rows > 0;
        }

        // True if the person is already a customer or a staff user
        public static bool IsPersonLinked(int personID)
        {
            string sql = @"SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Customers WHERE PersonID = @PersonID)
                                         OR EXISTS (SELECT 1 FROM dbo.Users     WHERE PersonID = @PersonID)
                                       THEN 1 ELSE 0 END;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID);

            return Convert.ToInt32(result) == 1;
        }

        // True if another person (not excludePersonID) already uses this email
        public static bool IsEmailUsed(string email, int excludePersonID)
        {
            string sql = "SELECT COUNT(1) FROM dbo.People WHERE Email = @Email AND PersonID <> @ExcludeID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;
                cmd.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = excludePersonID;
            });

            return Convert.ToInt32(result) > 0;
        }

        private static void AddPersonParameters(SqlCommand cmd, string firstName, string lastName, string phone,
                                                string? email, string? address, DateTime? dateOfBirth)
        {
            cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = firstName;
            cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = lastName;
            cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = phone;
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = (object?)email ?? DBNull.Value;
            cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 250).Value = (object?)address ?? DBNull.Value;
            cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value =
                dateOfBirth.HasValue ? (object)dateOfBirth.Value.Date : DBNull.Value;
        }
    }
}