using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    public static class clsPaymentData
    {
        // All payments with the customer name. method = "" means all methods.
        public static DataTable GetAllPayments(string search, string method)
        {
            string sql = @"SELECT pay.PaymentID, pay.OrderID,
                                  pe.FirstName + N' ' + pe.LastName AS CustomerName,
                                  pay.PaymentDate, pay.Method, pay.Amount, u.Username AS ReceivedBy
                           FROM dbo.Payments pay
                           JOIN dbo.Orders o     ON o.OrderID     = pay.OrderID
                           JOIN dbo.Customers cu ON cu.CustomerID = o.CustomerID
                           JOIN dbo.People pe    ON pe.PersonID   = cu.PersonID
                           JOIN dbo.Users u      ON u.UserID      = pay.ReceivedByUserID
                           WHERE (@Method = N'' OR pay.Method = @Method)
                             AND ((pe.FirstName + N' ' + pe.LastName) LIKE @Pattern
                                  OR CAST(pay.OrderID AS nvarchar(20)) LIKE @Pattern)
                           ORDER BY pay.PaymentDate DESC, pay.PaymentID DESC;";

            return clsDbHelper.GetTable(sql, cmd =>
            {
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = "%" + search.Trim() + "%";
                cmd.Parameters.Add("@Method", SqlDbType.NVarChar, 20).Value = method;
            });
        }

        // Orders that can still receive a payment (not cancelled, something left to pay)
        public static DataTable GetPayableOrders()
        {
            string sql = @"SELECT OrderID, CustomerName, TotalAmount, PaidAmount, RemainingAmount
                           FROM dbo.vw_OrderSummary
                           WHERE Status <> N'Cancelled' AND RemainingAmount > 0
                           ORDER BY OrderID DESC;";

            return clsDbHelper.GetTable(sql);
        }

        // Saves a payment. The order row is locked during the transaction, so two payments
        // for the same order can never pass the "not more than the balance" check at the same time.
        // Throws OrderRejectedException when the rules do not allow the payment.
        // Returns the new PaymentID.
        public static int AddPayment(int orderID, decimal amount, string method, int userID)
        {
            string lockOrder = @"SELECT Status, TotalAmount
                                 FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
                                 WHERE OrderID = @OrderID;";

            string sumPayments = "SELECT ISNULL(SUM(Amount), 0) FROM dbo.Payments WHERE OrderID = @OrderID;";

            string insertPayment = @"INSERT INTO dbo.Payments (OrderID, Amount, Method, ReceivedByUserID)
                                     VALUES (@OrderID, @Amount, @Method, @UserID);
                                     SELECT CAST(SCOPE_IDENTITY() AS int);";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1) Lock the order and read its status and total
                        string status;
                        decimal total;

                        using (SqlCommand command = new SqlCommand(lockOrder, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new OrderRejectedException("This order no longer exists.");

                                status = (string)reader["Status"];
                                total = (decimal)reader["TotalAmount"];
                            }
                        }

                        // 2) What has been paid so far
                        decimal paid;
                        using (SqlCommand command = new SqlCommand(sumPayments, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;
                            paid = Convert.ToDecimal(command.ExecuteScalar());
                        }

                        // 3) The rules
                        if (status == "Cancelled")
                            throw new OrderRejectedException("This order is cancelled and cannot receive payments.");

                        decimal remaining = total - paid;
                        if (amount > remaining)
                            throw new OrderRejectedException(
                                "The amount (" + amount.ToString("N2") + ") is more than the remaining balance (" +
                                remaining.ToString("N2") + ").");

                        // 4) Save the payment
                        int paymentID;
                        using (SqlCommand command = new SqlCommand(insertPayment, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int).Value = orderID;

                            SqlParameter amountParameter = command.Parameters.Add("@Amount", SqlDbType.Decimal);
                            amountParameter.Precision = 18;
                            amountParameter.Scale = 2;
                            amountParameter.Value = amount;

                            command.Parameters.Add("@Method", SqlDbType.NVarChar, 20).Value = method;
                            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userID;

                            paymentID = Convert.ToInt32(command.ExecuteScalar());
                        }

                        transaction.Commit();
                        return paymentID;
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