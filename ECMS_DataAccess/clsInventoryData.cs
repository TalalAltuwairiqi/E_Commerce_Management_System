using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    // Thrown when a stock change is not allowed (for example it would make the stock negative).
    public class InventoryRejectedException : Exception
    {
        public InventoryRejectedException(string message) : base(message) { }
    }

    public static class clsInventoryData
    {
        // Stock history, newest first (at most 1000 rows).
        // productID = 0 means all products. type = "" means all movement types.
        public static DataTable GetMovements(int productID, string type)
        {
            string sql = @"SELECT TOP (1000)
                                  sm.MovementID, sm.MovementDate, p.Name AS ProductName,
                                  sm.MovementType, sm.QuantityChange, sm.OrderID,
                                  u.Username AS UserName, sm.Notes
                           FROM dbo.StockMovements sm
                           JOIN dbo.Products p ON p.ProductID = sm.ProductID
                           JOIN dbo.Users u    ON u.UserID    = sm.UserID
                           WHERE (@ProductID = 0 OR sm.ProductID = @ProductID)
                             AND (@Type = N'' OR sm.MovementType = @Type)
                           ORDER BY sm.MovementDate DESC, sm.MovementID DESC;";

            return clsDbHelper.GetTable(sql, cmd =>
            {
                cmd.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 20).Value = type;
            });
        }

        // Uses the view created in 03_views.sql
        public static DataTable GetLowStock()
        {
            string sql = @"SELECT ProductID, Name, Category, StockQuantity, ReorderLevel
                           FROM dbo.vw_LowStock
                           ORDER BY StockQuantity, Name;";

            return clsDbHelper.GetTable(sql);
        }

        // Changes the stock AND writes the stock history in ONE transaction.
        // quantityChange is signed: positive adds stock, negative removes stock.
        // Returns the new stock. Throws InventoryRejectedException if the stock would become negative.
        public static int AdjustStock(int productID, int quantityChange, string movementType, int userID, string notes)
        {
            string updateStock = @"UPDATE dbo.Products
                                   SET StockQuantity = StockQuantity + @Change
                                   OUTPUT inserted.StockQuantity
                                   WHERE ProductID = @ProductID
                                     AND StockQuantity + @Change >= 0;";

            string insertMovement = @"INSERT INTO dbo.StockMovements
                                          (ProductID, QuantityChange, MovementType, OrderID, UserID, Notes)
                                      VALUES
                                          (@ProductID, @Change, @Type, NULL, @UserID, @Notes);";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int newStock;

                        using (SqlCommand command = new SqlCommand(updateStock, connection, transaction))
                        {
                            command.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                            command.Parameters.Add("@Change", SqlDbType.Int).Value = quantityChange;

                            object? result = command.ExecuteScalar();
                            if (result == null)
                                throw new InventoryRejectedException(
                                    "The stock could not be changed. The product does not exist, or the stock would become negative.");

                            newStock = Convert.ToInt32(result);
                        }

                        using (SqlCommand command = new SqlCommand(insertMovement, connection, transaction))
                        {
                            command.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                            command.Parameters.Add("@Change", SqlDbType.Int).Value = quantityChange;
                            command.Parameters.Add("@Type", SqlDbType.NVarChar, 20).Value = movementType;
                            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                            command.Parameters.Add("@Notes", SqlDbType.NVarChar, 250).Value = notes;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return newStock;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}