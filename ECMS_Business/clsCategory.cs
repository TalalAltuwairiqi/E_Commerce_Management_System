using System;
using System.Data;
using ECMS_DataAccess;

namespace ECMS_Business
{
    public class clsCategory
    {
        public int CategoryID { get; private set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsNew { get; private set; } = true;

        // New category
        public clsCategory() { }

        // Category loaded from the database
        private clsCategory(DataRow row)
        {
            CategoryID = (int)row["CategoryID"];
            Name = (string)row["Name"];
            Description = row.IsNull("Description") ? null : (string)row["Description"];
            IsNew = false;
        }

        public static clsCategory? Find(int categoryID)
        {
            DataTable table = clsCategoryData.GetCategoryByID(categoryID);
            return table.Rows.Count == 0 ? null : new clsCategory(table.Rows[0]);
        }

        public static DataTable GetAll(string search)
        {
            return clsCategoryData.GetAllCategories(search ?? string.Empty);
        }

        // ID + name only (for drop-down lists)
        public static DataTable GetNames()
        {
            return clsCategoryData.GetCategoryNames();
        }

        public bool Save(out string error)
        {
            Name = (Name ?? string.Empty).Trim();
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

            if (Name.Length == 0) { error = "Category name is required."; return false; }
            if (Name.Length > 50) { error = "Category name must be 50 characters or fewer."; return false; }
            if (Description != null && Description.Length > 250) { error = "Description must be 250 characters or fewer."; return false; }

            if (clsCategoryData.IsNameUsed(Name, IsNew ? 0 : CategoryID))
            {
                error = "A category with this name already exists.";
                return false;
            }

            if (IsNew)
            {
                int newID = clsCategoryData.AddNewCategory(Name, Description);
                if (newID <= 0)
                {
                    error = "The category could not be saved.";
                    return false;
                }

                CategoryID = newID;
                IsNew = false;
                error = string.Empty;
                return true;
            }

            if (!clsCategoryData.UpdateCategory(CategoryID, Name, Description))
            {
                error = "The category could not be updated.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        // Only an Admin can delete, and only categories without products.
        public static bool Delete(int categoryID, clsUser? actingUser, out string error)
        {
            if (actingUser == null || !actingUser.IsAdmin)
            {
                error = "Only an administrator can delete categories.";
                return false;
            }

            int productCount = clsCategoryData.CountProducts(categoryID);
            if (productCount > 0)
            {
                error = "This category has " + productCount + " product(s) and cannot be deleted.";
                return false;
            }

            if (!clsCategoryData.DeleteCategory(categoryID))
            {
                error = "The category could not be deleted.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}