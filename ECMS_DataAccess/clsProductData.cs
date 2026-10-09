using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    public static class clsProductData
    {
        // categoryID = 0 means "all categories"
        public static DataTable GetAllProducts(string search, int categoryID, bool activeOnly, bool lowStockOnly)
        {
            string sql = @"SELECT p.ProductID, p.Name, p.CategoryID, c.Name AS Category,
                                  p.Price, p.StockQuantity, p.ReorderLevel, p.IsActive
                           FROM dbo.Products p
                           JOIN dbo.Categories c ON c.CategoryID = p.CategoryID
                           WHERE (@CategoryID = 0 OR p.CategoryID = @CategoryID)
                             AND (@ActiveOnly = 0 OR p.IsActive = 1)
                             AND (@LowStockOnly = 0 OR p.StockQuantity <= p.ReorderLevel)
                             AND (p.Name LIKE @Pattern OR p.Description LIKE @Pattern OR c.Name LIKE @Pattern)
                           ORDER BY p.Name;";

            return clsDbHelper.GetTable(sql, cmd =>
            {
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%";
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly;
                cmd.Parameters.Add("@LowStockOnly", SqlDbType.Bit).Value = lowStockOnly;
            });
        }

        public static DataTable GetProductByID(int productID)
        {
            string sql = @"SELECT ProductID, Name, Description, Price, StockQuantity, ReorderLevel,
                                  CategoryID, IsActive, CreatedAt
                           FROM dbo.Products
                           WHERE ProductID = @ProductID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID);
        }

        // Adds the product. If it starts with some stock, a "Restock" movement is logged too.
        // Both inserts run in ONE transaction: either both succeed or neither is saved.
        // Returns the new ProductID.
        public static int AddNewProduct(string name, string? description, decimal price, int reorderLevel,
                                        int categoryID, bool isActive, int initialStock, int userID)
        {
            string insertProduct = @"INSERT INTO dbo.Products
                                         (Name, Description, Price, StockQuantity, ReorderLevel, CategoryID, IsActive)
                                     VALUES
                                         (@Name, @Description, @Price, @Stock, @ReorderLevel, @CategoryID, @IsActive);
                                     SELECT CAST(SCOPE_IDENTITY() AS int);";

            string insertMovement = @"INSERT INTO dbo.StockMovements
                                          (ProductID, QuantityChange, MovementType, OrderID, UserID, Notes)
                                      VALUES
                                          (@ProductID, @Quantity, N'Restock', NULL, @UserID, N'Initial stock');";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int productID;

                        using (SqlCommand command = new SqlCommand(insertProduct, connection, transaction))
                        {
                            command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                            command.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value = (object?)description ?? DBNull.Value;
                            AddPriceParameter(command, price);
                            command.Parameters.Add("@Stock", SqlDbType.Int).Value = initialStock;
                            command.Parameters.Add("@ReorderLevel", SqlDbType.Int).Value = reorderLevel;
                            command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;

                            productID = Convert.ToInt32(command.ExecuteScalar());
                        }

                        if (initialStock > 0)
                        {
                            using (SqlCommand command = new SqlCommand(insertMovement, connection, transaction))
                            {
                                command.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                                command.Parameters.Add("@Quantity", SqlDbType.Int).Value = initialStock;
                                command.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return productID;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // The stock is NOT changed here: stock only changes through stock movements.
        public static bool UpdateProduct(int productID, string name, string? description, decimal price,
                                         int reorderLevel, int categoryID, bool isActive)
        {
            string sql = @"UPDATE dbo.Products
                           SET Name = @Name, Description = @Description, Price = @Price,
                               ReorderLevel = @ReorderLevel, CategoryID = @CategoryID, IsActive = @IsActive
                           WHERE ProductID = @ProductID;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value = (object?)description ?? DBNull.Value;
                AddPriceParameter(cmd, price);
                cmd.Parameters.Add("@ReorderLevel", SqlDbType.Int).Value = reorderLevel;
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
            });

            return rows > 0;
        }

        public static bool IsNameUsedInCategory(string name, int categoryID, int excludeProductID)
        {
            string sql = @"SELECT COUNT(1) FROM dbo.Products
                           WHERE Name = @Name AND CategoryID = @CategoryID AND ProductID <> @ExcludeID;";

            object? result = clsDbHelper.ExecuteScalar(sql, cmd =>
            {
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                cmd.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = excludeProductID;
            });

            return Convert.ToInt32(result) > 0;
        }

        // Money values are always sent as decimal(18,2)
        private static void AddPriceParameter(SqlCommand command, decimal price)
        {
            SqlParameter parameter = command.Parameters.Add("@Price", SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = price;
        }
    }
}