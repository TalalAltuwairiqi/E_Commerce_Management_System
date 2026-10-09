using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    // Thrown when an order cannot be saved because of the data (for example not enough stock).
    // The Business layer catches it and shows the message to the user.
    public class OrderRejectedException : Exception
    {
        public OrderRejectedException(string message) : base(message) { }
    }

    public static class clsOrderData
    {
        // Creates the order, its items and the stock movements in ONE transaction.
        // If anything fails, everything is rolled back and nothing is saved.
        // Returns the new OrderID.
        public static int CreateOrder(int customerID, int userID, string? notes, decimal taxRate,
                                      IList<(int ProductID, int Quantity)> items)
        {
            string insertOrder = @"INSERT INTO dbo.Orders
                                       (CustomerID, CreatedByUserID, Status, Subtotal, TaxAmount, TotalAmount, Notes)
                                   VALUES
                                       (@CustomerID, @UserID, N'Pending', 0, 0, 0, @Notes);
                                   SELECT CAST(SCOPE_IDENTITY() AS int);";

            // Takes the stock and returns the CURRENT price in one atomic statement.
            // If the product is inactive or the stock is not enough, no row is updated and nothing is returned.
            string reduceStock = @"UPDATE dbo.Products
                                   SET StockQuantity = StockQuantity - @Quantity
                                   OUTPUT inserted.Price
                                   WHERE ProductID = @ProductID
                                     AND IsActive = 1
                                     AND StockQuantity >= @Quantity;";

            string insertItem = @"INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity, UnitPrice)
                                  VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice);";

            string insertMovement = @"INSERT INTO dbo.StockMovements
                                          (ProductID, QuantityChange, MovementType, OrderID, UserID, Notes)
                                      VALUES
                                          (@ProductID, @QuantityChange, N'Sale', @OrderID, @UserID, N'Order sale');";

            string updateTotals = @"UPDATE dbo.Orders
                                    SET Subtotal = @Subtotal, TaxAmount = @TaxAmount, TotalAmount = @TotalAmount
                                    WHERE OrderID = @OrderID;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1) The order itself
                        int orderID;
                        using (SqlCommand command = new SqlCommand(insertOrder, connection, transaction))
                        {
                            command.Parameters.Add("@CustomerID", SqlDbType.Int).Value = customerID;
                            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                            command.Parameters.Add("@Notes", SqlDbType.NVarChar, 250).Value = (object?)notes ?? DBNull.Value;
                            orderID = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // 2) Every item: take the stock, save the item, log the movement
                        decimal subtotal = 0;

                        foreach ((int productID, int quantity) in items)
                        {
                            decimal unitPrice;

                            using (SqlCommand command = new SqlCommand(reduceStock, connection, transaction))
                            {
                                command.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                                command.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;

                                object? price = command.ExecuteScalar();
                                if (price == null)
                                    throw new OrderRejectedException(
                                        "Product #" + productID + " is not active or does not have enough stock. The order was not saved.");

                                unitPrice = Convert.ToDecimal(price);
                            }

                            using (SqlCommand command = new SqlCommand(insertItem, connection, transaction))
                            {
                                command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                                command.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                                command.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;
                                AddMoneyParameter(command, "@UnitPrice", unitPrice);
                                command.ExecuteNonQuery();
                            }

                            using (SqlCommand command = new SqlCommand(insertMovement, connection, transaction))
                            {
                                command.Parameters.Add("@ProductID", SqlDbType.Int).Value = productID;
                                command.Parameters.Add("@QuantityChange", SqlDbType.Int).Value = -quantity;
                                command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                                command.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                                command.ExecuteNonQuery();
                            }

                            subtotal += unitPrice * quantity;
                        }

                        // 3) Totals, calculated from the real prices that were just read
                        decimal taxAmount = decimal.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);

                        using (SqlCommand command = new SqlCommand(updateTotals, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                            AddMoneyParameter(command, "@Subtotal", subtotal);
                            AddMoneyParameter(command, "@TaxAmount", taxAmount);
                            AddMoneyParameter(command, "@TotalAmount", subtotal + taxAmount);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return orderID;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // One order with customer name, paid and remaining amounts (uses the view vw_OrderSummary)
        public static DataTable GetOrderSummary(int orderID)
        {
            string sql = @"SELECT OrderID, CustomerName, OrderDate, Status,
                                  Subtotal, TaxAmount, TotalAmount, PaidAmount, RemainingAmount
                           FROM dbo.vw_OrderSummary
                           WHERE OrderID = @OrderID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID);
        }

        // ------------------------------------------------------------------
        // Stage 5B: listing orders, order details, status changes, cancelling
        // ------------------------------------------------------------------

        // Orders list. status = "" means all statuses.
        public static DataTable GetOrders(string search, string status, bool unpaidOnly)
        {
            string sql = @"SELECT OrderID, CustomerName, CreatedBy, OrderDate, Status,
                                  TotalAmount, PaidAmount, RemainingAmount
                           FROM dbo.vw_OrderSummary
                           WHERE (@Status = N'' OR Status = @Status)
                             AND (@UnpaidOnly = 0 OR (RemainingAmount > 0 AND Status <> N'Cancelled'))
                             AND (CustomerName LIKE @Pattern OR CAST(OrderID AS nvarchar(20)) LIKE @Pattern)
                           ORDER BY OrderID DESC;";

            return clsDbHelper.GetTable(sql, cmd =>
            {
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%";
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status;
                cmd.Parameters.Add("@UnpaidOnly", SqlDbType.Bit).Value = unpaidOnly;
            });
        }

        // Everything about one order except its items and payments
        public static DataTable GetOrderHeader(int orderID)
        {
            string sql = @"SELECT s.OrderID, s.CustomerID, s.CustomerName, s.CreatedBy, s.OrderDate, s.Status,
                                  s.Subtotal, s.TaxAmount, s.TotalAmount, s.PaidAmount, s.RemainingAmount,
                                  o.Notes
                           FROM dbo.vw_OrderSummary s
                           JOIN dbo.Orders o ON o.OrderID = s.OrderID
                           WHERE s.OrderID = @OrderID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID);
        }

        // Uses the stored procedure created in 04_procedures.sql
        public static DataTable GetOrderItems(int orderID)
        {
            return clsDbHelper.GetTable("dbo.sp_GetOrderItems", cmd =>
                cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID,
                CommandType.StoredProcedure);
        }

        public static DataTable GetOrderPayments(int orderID)
        {
            string sql = @"SELECT pay.PaymentID, pay.PaymentDate, pay.Method, pay.Amount, u.Username AS ReceivedBy
                           FROM dbo.Payments pay
                           JOIN dbo.Users u ON u.UserID = pay.ReceivedByUserID
                           WHERE pay.OrderID = @OrderID
                           ORDER BY pay.PaymentDate, pay.PaymentID;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID);
        }

        // Changes the status only if it is still the status the caller saw (protects against two users at once).
        public static bool UpdateOrderStatus(int orderID, string expectedStatus, string newStatus)
        {
            string sql = @"UPDATE dbo.Orders SET Status = @NewStatus
                           WHERE OrderID = @OrderID AND Status = @ExpectedStatus;";

            int rows = clsDbHelper.ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                cmd.Parameters.Add("@ExpectedStatus", SqlDbType.NVarChar, 20).Value = expectedStatus;
                cmd.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = newStatus;
            });

            return rows > 0;
        }

        // Cancels the order, returns the stock and logs it, all in ONE transaction.
        // Throws OrderRejectedException when the rules do not allow the cancellation.
        public static void CancelOrder(int orderID, int userID)
        {
            string cancelOrder = @"UPDATE dbo.Orders SET Status = N'Cancelled'
                                   WHERE OrderID = @OrderID AND Status IN (N'Pending', N'Processing');";

            string countPayments = "SELECT COUNT(1) FROM dbo.Payments WHERE OrderID = @OrderID;";

            string restoreStock = @"UPDATE p
                                    SET p.StockQuantity = p.StockQuantity + oi.Quantity
                                    FROM dbo.Products p
                                    JOIN dbo.OrderItems oi ON oi.ProductID = p.ProductID
                                    WHERE oi.OrderID = @OrderID;";

            string logMovements = @"INSERT INTO dbo.StockMovements
                                        (ProductID, QuantityChange, MovementType, OrderID, UserID, Notes)
                                    SELECT ProductID, Quantity, N'Cancellation', OrderID, @UserID, N'Order cancelled'
                                    FROM dbo.OrderItems
                                    WHERE OrderID = @OrderID;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1) Change the status (only possible from Pending or Processing)
                        int changed;
                        using (SqlCommand command = new SqlCommand(cancelOrder, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                            changed = command.ExecuteNonQuery();
                        }

                        if (changed == 0)
                            throw new OrderRejectedException(
                                "This order can no longer be cancelled. Its status has changed.");

                        // 2) Refunds are not supported, so an order with payments cannot be cancelled
                        using (SqlCommand command = new SqlCommand(countPayments, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                            if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                                throw new OrderRejectedException(
                                    "This order has payments. Refunds are not supported in this version, so it cannot be cancelled.");
                        }

                        // 3) Give the stock back
                        using (SqlCommand command = new SqlCommand(restoreStock, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                            command.ExecuteNonQuery();
                        }

                        // 4) Write it in the stock history
                        using (SqlCommand command = new SqlCommand(logMovements, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void AddMoneyParameter(SqlCommand command, string name, decimal value)
        {
            SqlParameter parameter = command.Parameters.Add(name, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = value;
        }
    }
}