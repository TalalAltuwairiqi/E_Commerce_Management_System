using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    // Data Access Layer: ADO.NET only. Returns raw data; the Business layer decides what it means.
    public static class clsUserData
    {
        // ------------------------------------------------------------------
        // Used by the login (Stage 3A)
        // ------------------------------------------------------------------
        public static bool GetUserByUsername(string username,
            out int userID, out int personID, out string passwordHash,
            out string role, out bool isActive, out string fullName)
        {
            userID = 0;
            personID = 0;
            passwordHash = string.Empty;
            role = string.Empty;
            isActive = false;
            fullName = string.Empty;

            string query = @"SELECT u.UserID, u.PersonID, u.PasswordHash, u.Role, u.IsActive,
                                    p.FirstName + N' ' + p.LastName AS FullName
                             FROM dbo.Users u
                             JOIN dbo.People p ON p.PersonID = u.PersonID
                             WHERE u.Username = @Username;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Parameters protect against SQL injection.
                command.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return false;

                    userID = (int)reader["UserID"];
                    personID = (int)reader["PersonID"];
                    passwordHash = (string)reader["PasswordHash"];
                    role = (string)reader["Role"];
                    isActive = (bool)reader["IsActive"];
                    fullName = (string)reader["FullName"];
                    return true;
                }
            }
        }

        // ------------------------------------------------------------------
        // User management (Stage 3C)
        // ------------------------------------------------------------------
        public static DataTable GetAllUsers(string search, bool activeOnly)
        {
            string sql = @"SELECT u.UserID, u.PersonID,
                                  p.FirstName + N' ' + p.LastName AS FullName,
                                  u.Username, u.Role, u.IsActive, u.CreatedAt
                           FROM dbo.Users u
                           JOIN dbo.People p ON p.PersonID = u.PersonID
                           WHERE (@ActiveOnly = 0 OR u.IsActive = 1)
                             AND (u.Username LIKE @Pattern
                                  OR u.Role LIKE @Pattern
                                  OR p.FirstName LIKE @Pattern
                                  OR p.LastName LIKE @Pattern
                                  OR (p.FirstName + N' ' + p.LastName) LIKE @Pattern)
                           ORDER BY u.Username;";

            return clsDbHelper.GetTable(sql, cmd =>
            {
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%";
                cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly;
            });
        }

        public static DataTable GetUserByID(int userID)
        {
            string sql = @"SELECT u.UserID, u.PersonID, u.Username, u.Role, u.IsActive, u.CreatedAt,
                                  p.FirstName + N' ' + p.LastName AS FullName
                           FROM dbo.Users u
                           JOIN dbo.People p ON p.PersonID = u.PersonID
                           WHERE u.UserID = @UserID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userID);
        }

        // Returns the new UserID (0 if it failed)
        public static int AddNewUser(int personID, string username, string passwordHash, string role, bool isActive)
        {
            string sql = @"INSERT INTO dbo.Users (PersonID, Username, PasswordHash, Role, IsActive)
                           VALUES (@PersonID, @Username, @PasswordHash, @Role, @IsActive);
                           SELECT CAST(SCOPE_IDENTITY() AS int);";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 200).Value = passwordHash;
                cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = role;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
            });

            if (result != null && int.TryParse(result.ToString(), out int newID))
                return newID;

            return 0;
        }

        public static bool UpdateUser(int userID, string username, string role, bool isActive)
        {
            string sql = @"UPDATE dbo.Users
                           SET Username = @Username, Role = @Role, IsActive = @IsActive
                           WHERE UserID = @UserID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;
                cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = role;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
            });

            return rows > 0;
        }

        public static bool UpdatePassword(int userID, string passwordHash)
        {
            string sql = "UPDATE dbo.Users SET PasswordHash = @PasswordHash WHERE UserID = @UserID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 200).Value = passwordHash;
            });

            return rows > 0;
        }

        public static string? GetPasswordHash(int userID)
        {
            string sql = "SELECT PasswordHash FROM dbo.Users WHERE UserID = @UserID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userID);

            if (result == null || result == DBNull.Value)
                return null;

            return (string)result;
        }

        // True if another user (not excludeUserID) already uses this username
        public static bool IsUsernameUsed(string username, int excludeUserID)
        {
            string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Username = @Username AND UserID <> @ExcludeID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;
                cmd.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = excludeUserID;
            });

            return Convert.ToInt32(result) > 0;
        }

        public static bool IsPersonAlreadyUser(int personID)
        {
            string sql = "SELECT COUNT(1) FROM dbo.Users WHERE PersonID = @PersonID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                cmd.Parameters.Add("@PersonID", SqlDbType.Int).Value = personID);

            return Convert.ToInt32(result) > 0;
        }

        // Number of active Admin accounts, not counting one user
        public static int CountActiveAdminsExcluding(int excludeUserID)
        {
            string sql = @"SELECT COUNT(1) FROM dbo.Users
                           WHERE Role = N'Admin' AND IsActive = 1 AND UserID <> @ExcludeID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                cmd.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = excludeUserID);

            return Convert.ToInt32(result);
        }

        // People who do not have a user account yet (used by the "Add User" form)
        public static DataTable GetPeopleWithoutUser()
        {
            string sql = @"SELECT p.PersonID,
                                  p.FirstName + N' ' + p.LastName + N'  (' + p.Phone + N')' AS DisplayName
                           FROM dbo.People p
                           WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.PersonID = p.PersonID)
                           ORDER BY p.FirstName, p.LastName;";

            return clsDbHelper.GetTable(sql);
        }
    }
}