using System;
using System.Data;
using ECMS_DataAccess;

namespace ECMS_Business
{
    // The three ways staff can change the stock by hand
    public enum enStockOperation
    {
        Restock = 0,              // stock received from a supplier (adds)
        IncreaseCorrection = 1,   // a count showed more stock than the system (adds)
        DecreaseCorrection = 2    // damaged, lost or counted less (removes)
    }

    // Business Layer: the rules for changing the stock by hand.
    public static class clsInventory
    {
        private const int MaxQuantity = 1000000;
        private const int MaxStock = 10000000;

        public static readonly string[] MovementTypes = { "Sale", "Cancellation", "Adjustment", "Restock" };

        // productID = 0 means all products, type = "" means all types
        public static DataTable GetMovements(int productID, string type)
        {
            return clsInventoryData.GetMovements(productID, type ?? string.Empty);
        }

        public static DataTable GetLowStock()
        {
            return clsInventoryData.GetLowStock();
        }

        public static bool Adjust(int productID, enStockOperation operation, int quantity, string reason,
                                  clsUser? actingUser, out int newStock, out string error)
        {
            newStock = 0;

            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            if (quantity <= 0 || quantity > MaxQuantity)
            {
                error = "Quantity must be between 1 and " + MaxQuantity.ToString("N0") + ".";
                return false;
            }

            reason = (reason ?? string.Empty).Trim();
            if (reason.Length < 3)
            {
                error = "Please write a reason (at least 3 characters). It is saved in the stock history.";
                return false;
            }

            if (reason.Length > 250)
            {
                error = "The reason must be 250 characters or fewer.";
                return false;
            }

            clsProduct? product = clsProduct.Find(productID);
            if (product == null)
            {
                error = "This product no longer exists.";
                return false;
            }

            int change = operation == enStockOperation.DecreaseCorrection ? -quantity : quantity;
            string movementType = operation == enStockOperation.Restock ? "Restock" : "Adjustment";

            if (product.StockQuantity + change < 0)
            {
                error = "Only " + product.StockQuantity + " unit(s) of '" + product.Name + "' are in stock. You cannot remove " + quantity + ".";
                return false;
            }

            if ((long)product.StockQuantity + change > MaxStock)
            {
                error = "The stock cannot be more than " + MaxStock.ToString("N0") + ".";
                return false;
            }

            try
            {
                newStock = clsInventoryData.AdjustStock(productID, change, movementType, actingUser.UserID, reason);
            }
            catch (InventoryRejectedException ex)
            {
                error = ex.Message;
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}