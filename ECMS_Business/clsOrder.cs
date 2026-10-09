using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ECMS_DataAccess;

namespace ECMS_Business
{
    // One line of an order (before it is saved)
    public class clsOrderItem
    {
        public int ProductID { get; }
        public string ProductName { get; }
        public decimal UnitPrice { get; }
        public int Quantity { get; internal set; }
        public decimal LineTotal => UnitPrice * Quantity;

        public clsOrderItem(int productID, string productName, decimal unitPrice, int quantity)
        {
            ProductID = productID;
            ProductName = productName;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }
    }

    // Business Layer: builds an order in memory, calculates the totals and saves it.
    public class clsOrder
    {
        public int OrderID { get; private set; }
        public int CustomerID { get; set; }
        public string? Notes { get; set; }
        public List<clsOrderItem> Items { get; } = new List<clsOrderItem>();

        // The total as saved in the database (it can differ from the screen if a price changed meanwhile)
        public decimal SavedTotalAmount { get; private set; }

        public bool IsNew => OrderID == 0;

        public decimal TaxRate => clsAppSettings.TaxRate;
        public decimal Subtotal => Items.Sum(i => i.LineTotal);
        public decimal TaxAmount => decimal.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        public decimal TotalAmount => Subtotal + TaxAmount;

        // Adds a product. If it is already in the order, the quantity is increased.
        public bool AddItem(clsProduct product, int quantity, out string error)
        {
            if (!product.IsActive)
            {
                error = "'" + product.Name + "' is not active and cannot be sold.";
                return false;
            }

            if (quantity <= 0)
            {
                error = "Quantity must be greater than zero.";
                return false;
            }

            clsOrderItem? existing = Items.FirstOrDefault(i => i.ProductID == product.ProductID);
            int newQuantity = (existing?.Quantity ?? 0) + quantity;

            if (newQuantity > product.StockQuantity)
            {
                error = "Only " + product.StockQuantity + " unit(s) of '" + product.Name + "' are in stock.";
                return false;
            }

            if (existing != null)
                existing.Quantity = newQuantity;
            else
                Items.Add(new clsOrderItem(product.ProductID, product.Name, product.Price, quantity));

            error = string.Empty;
            return true;
        }

        public void RemoveItem(int productID)
        {
            Items.RemoveAll(i => i.ProductID == productID);
        }

        public bool Save(clsUser? actingUser, out string error)
        {
            if (!IsNew)
            {
                error = "This order was already saved.";
                return false;
            }

            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            if (Items.Count == 0)
            {
                error = "Add at least one product to the order.";
                return false;
            }

            clsCustomer? customer = CustomerID > 0 ? clsCustomer.Find(CustomerID) : null;
            if (customer == null)
            {
                error = "Please select a customer.";
                return false;
            }

            if (!customer.IsActive)
            {
                error = "This customer is not active.";
                return false;
            }

            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            if (Notes != null && Notes.Length > 250)
            {
                error = "Notes must be 250 characters or fewer.";
                return false;
            }

            List<(int ProductID, int Quantity)> lines = Items.Select(i => (i.ProductID, i.Quantity)).ToList();

            try
            {
                OrderID = clsOrderData.CreateOrder(CustomerID, actingUser.UserID, Notes, TaxRate, lines);
            }
            catch (OrderRejectedException ex)
            {
                error = ex.Message;
                return false;
            }

            DataTable summary = clsOrderData.GetOrderSummary(OrderID);
            SavedTotalAmount = summary.Rows.Count > 0 ? (decimal)summary.Rows[0]["TotalAmount"] : TotalAmount;

            error = string.Empty;
            return true;
        }

        // ------------------------------------------------------------------
        // Stage 5B: finding orders, status workflow, cancelling
        // ------------------------------------------------------------------

        public static readonly string[] Statuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        // The next step of the normal flow (null when there is no next step)
        public static string? GetNextStatus(string currentStatus)
        {
            switch (currentStatus)
            {
                case "Pending": return "Processing";
                case "Processing": return "Shipped";
                case "Shipped": return "Delivered";
                default: return null;
            }
        }

        // An order can be cancelled only before it is shipped
        public static bool CanCancel(string status)
        {
            return status == "Pending" || status == "Processing";
        }

        // status = "" means all statuses
        public static DataTable GetAll(string search, string status, bool unpaidOnly)
        {
            return clsOrderData.GetOrders(search ?? string.Empty, status ?? string.Empty, unpaidOnly);
        }

        public static DataTable GetHeader(int orderID)
        {
            return clsOrderData.GetOrderHeader(orderID);
        }

        public static DataTable GetItems(int orderID)
        {
            return clsOrderData.GetOrderItems(orderID);
        }

        public static DataTable GetPayments(int orderID)
        {
            return clsOrderData.GetOrderPayments(orderID);
        }

        // Moves the order one step forward: Pending -> Processing -> Shipped -> Delivered
        public static bool ChangeStatus(int orderID, string newStatus, clsUser? actingUser, out string error)
        {
            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            DataTable header = clsOrderData.GetOrderHeader(orderID);
            if (header.Rows.Count == 0)
            {
                error = "This order no longer exists.";
                return false;
            }

            string currentStatus = (string)header.Rows[0]["Status"];

            if (GetNextStatus(currentStatus) != newStatus)
            {
                error = "An order cannot move from '" + currentStatus + "' to '" + newStatus + "'.";
                return false;
            }

            if (!clsOrderData.UpdateOrderStatus(orderID, currentStatus, newStatus))
            {
                error = "The order was changed by someone else. Please refresh and try again.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        // Cancels an order and returns its stock
        public static bool Cancel(int orderID, clsUser? actingUser, out string error)
        {
            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            DataTable header = clsOrderData.GetOrderHeader(orderID);
            if (header.Rows.Count == 0)
            {
                error = "This order no longer exists.";
                return false;
            }

            string status = (string)header.Rows[0]["Status"];
            if (!CanCancel(status))
            {
                error = "An order that is '" + status + "' cannot be cancelled.";
                return false;
            }

            try
            {
                clsOrderData.CancelOrder(orderID, actingUser.UserID);
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