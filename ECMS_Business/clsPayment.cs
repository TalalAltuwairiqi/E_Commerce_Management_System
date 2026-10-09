using System;
using System.Data;
using System.Linq;
using ECMS_DataAccess;

namespace ECMS_Business
{
    // Business Layer: the rules for taking a payment.
    public static class clsPayment
    {
        public static readonly string[] Methods = { "Cash", "Card", "Transfer" };

        // method = "" means all methods
        public static DataTable GetAll(string search, string method)
        {
            return clsPaymentData.GetAllPayments(search ?? string.Empty, method ?? string.Empty);
        }

        public static DataTable GetPayableOrders()
        {
            return clsPaymentData.GetPayableOrders();
        }

        public static bool Add(int orderID, decimal amount, string method, clsUser? actingUser, out string error)
        {
            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

            if (amount <= 0)
            {
                error = "The amount must be greater than zero.";
                return false;
            }

            if (!Methods.Contains(method))
            {
                error = "Please choose a payment method.";
                return false;
            }

            // Friendly checks first (the database repeats them inside the transaction)
            DataTable header = clsOrderData.GetOrderHeader(orderID);
            if (header.Rows.Count == 0)
            {
                error = "This order no longer exists.";
                return false;
            }

            string status = (string)header.Rows[0]["Status"];
            if (status == "Cancelled")
            {
                error = "This order is cancelled and cannot receive payments.";
                return false;
            }

            decimal remaining = Convert.ToDecimal(header.Rows[0]["RemainingAmount"]);
            if (remaining <= 0)
            {
                error = "This order is already fully paid.";
                return false;
            }

            if (amount > remaining)
            {
                error = "The amount is more than the remaining balance (" + remaining.ToString("N2") + ").";
                return false;
            }

            try
            {
                clsPaymentData.AddPayment(orderID, amount, method, actingUser.UserID);
            }
            catch (OrderRejectedException ex)
            {
                error = ex.Message;
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}