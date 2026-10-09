using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    public static class clsCategoryData
    {
        // Categories with the number of products in each one
        public static DataTable GetAllCategories(string search)
        {
            string sql = @"SELECT c.CategoryID, c.Name, c.Description, COUNT(p.ProductID) AS ProductCount
                           FROM dbo.Categories c
                           LEFT JOIN dbo.Products p ON p.CategoryID = c.CategoryID
                           WHERE c.Name LIKE @Pattern OR c.Description LIKE @Pattern
                           GROUP BY c.CategoryID, c.Name, c.Description
                           ORDER BY c.Name;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%");
        }

        // Short list (ID + name) for drop-down lists
        public static DataTable GetCategoryNames()
        {
            return clsDbHelper.GetTable("SELECT CategoryID, Name FROM dbo.Categories ORDER BY Name;");
        }

        public static DataTable GetCategoryByID(int categoryID)
        {
            string sql = "SELECT CategoryID, Name, Description FROM dbo.Categories WHERE CategoryID = @CategoryID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID);
        }

        // Returns the new CategoryID (0 if it failed)
        public static int AddNewCategory(string name, string? description)
        {
            string sql = @"INSERT INTO dbo.Categories (Name, Description)
                           VALUES (@Name, @Description);
                           SELECT CAST(SCOPE_IDENTITY() AS int);";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = name;
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 250).Value = (object?)description ?? DBNull.Value;
            });

            if (result != null && int.TryParse(result.ToString(), out int newID))
                return newID;

            return 0;
        }

        public static bool UpdateCategory(int categoryID, string name, string? description)
        {
            string sql = @"UPDATE dbo.Categories
                           SET Name = @Name, Description = @Description
                           WHERE CategoryID = @CategoryID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = name;
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 250).Value = (object?)description ?? DBNull.Value;
            });

            return rows > 0;
        }

        public static bool DeleteCategory(int categoryID)
        {
            string sql = "DELETE FROM dbo.Categories WHERE CategoryID = @CategoryID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID);

            return rows > 0;
        }

        public static bool IsNameUsed(string name, int excludeCategoryID)
        {
            string sql = "SELECT COUNT(1) FROM dbo.Categories WHERE Name = @Name AND CategoryID <> @ExcludeID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = name;
                cmd.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = excludeCategoryID;
            });

            return Convert.ToInt32(result) > 0;
        }

        public static int CountProducts(int categoryID)
        {
            string sql = "SELECT COUNT(1) FROM dbo.Products WHERE CategoryID = @CategoryID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID);

            return Convert.ToInt32(result);
        }
    }
}