using System;
using System.Data;
using ECMS_DataAccess;

namespace ECMS_Business
{
    public class clsProduct
    {
        private const decimal MaxPrice = 1000000m;

        public int ProductID { get; private set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int ReorderLevel { get; set; } = 5;
        public int CategoryID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; private set; }
        public bool IsNew { get; private set; } = true;

        // Current stock. It can only be changed through stock movements (sales, restock, adjustments).
        public int StockQuantity { get; private set; }

        // Only used when a NEW product is created: the starting stock.
        public int InitialStock { get; set; }

        // New product
        public clsProduct() { }

        // Product loaded from the database
        private clsProduct(DataRow row)
        {
            ProductID = (int)row["ProductID"];
            Name = (string)row["Name"];
            Description = row.IsNull("Description") ? null : (string)row["Description"];
            Price = (decimal)row["Price"];
            StockQuantity = (int)row["StockQuantity"];
            ReorderLevel = (int)row["ReorderLevel"];
            CategoryID = (int)row["CategoryID"];
            IsActive = (bool)row["IsActive"];
            CreatedAt = (DateTime)row["CreatedAt"];
            IsNew = false;
        }

        public static clsProduct? Find(int productID)
        {
            DataTable table = clsProductData.GetProductByID(productID);
            return table.Rows.Count == 0 ? null : new clsProduct(table.Rows[0]);
        }

        // categoryID = 0 means all categories
        public static DataTable GetAll(string search, int categoryID, bool activeOnly, bool lowStockOnly)
        {
            return clsProductData.GetAllProducts(search ?? string.Empty, categoryID, activeOnly, lowStockOnly);
        }

        public bool Save(clsUser? actingUser, out string error)
        {
            if (actingUser == null)
            {
                error = "You must be logged in.";
                return false;
            }

            Name = (Name ?? string.Empty).Trim();
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            Price = decimal.Round(Price, 2);

            if (Name.Length == 0) { error = "Product name is required."; return false; }
            if (Name.Length > 100) { error = "Product name must be 100 characters or fewer."; return false; }
            if (Description != null && Description.Length > 1000) { error = "Description must be 1000 characters or fewer."; return false; }
            if (Price < 0 || Price > MaxPrice) { error = "Price must be between 0 and 1,000,000."; return false; }
            if (ReorderLevel < 0) { error = "Reorder level cannot be negative."; return false; }
            if (CategoryID <= 0) { error = "Please select a category."; return false; }
            if (IsNew && InitialStock < 0) { error = "Initial stock cannot be negative."; return false; }

            if (clsProductData.IsNameUsedInCategory(Name, CategoryID, IsNew ? 0 : ProductID))
            {
                error = "A product with this name already exists in this category.";
                return false;
            }

            if (IsNew)
            {
                int newID = clsProductData.AddNewProduct(Name, Description, Price, ReorderLevel,
                                                         CategoryID, IsActive, InitialStock, actingUser.UserID);
                if (newID <= 0)
                {
                    error = "The product could not be saved.";
                    return false;
                }

                ProductID = newID;
                StockQuantity = InitialStock;
                IsNew = false;
                error = string.Empty;
                return true;
            }

            if (!clsProductData.UpdateProduct(ProductID, Name, Description, Price, ReorderLevel, CategoryID, IsActive))
            {
                error = "The product could not be updated.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}