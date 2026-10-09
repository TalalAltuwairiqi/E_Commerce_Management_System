using System;
using System.Data;
using ECMS_DataAccess;

namespace ECMS_Business
{
    // The numbers shown on the main screen
    public class clsDashboardStats
    {
        public int ActiveCustomers { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }

        // Money figures are only filled for an Admin
        public decimal? Revenue { get; set; }
        public decimal? OutstandingBalance { get; set; }
    }

    // Business Layer: reports are for Admins only, and this is checked here (not only by hiding the menu).
    public static class clsReport
    {
        private static void RequireAdmin(clsUser? actingUser)
        {
            if (actingUser == null || !actingUser.IsAdmin)
                throw new UnauthorizedAccessException("Only an administrator can view reports.");
        }

        public static clsDashboardStats GetDashboard(clsUser? actingUser)
        {
            DataTable table = clsReportData.GetDashboardStats();
            DataRow row = table.Rows[0];

            clsDashboardStats stats = new clsDashboardStats
            {
                ActiveCustomers = Convert.ToInt32(row["ActiveCustomers"]),
                ActiveProducts = Convert.ToInt32(row["ActiveProducts"]),
                TotalOrders = Convert.ToInt32(row["TotalOrders"]),
                PendingOrders = Convert.ToInt32(row["PendingOrders"]),
                LowStockProducts = Convert.ToInt32(row["LowStockProducts"])
            };

            if (actingUser != null && actingUser.IsAdmin)
            {
                stats.Revenue = Convert.ToDecimal(row["Revenue"]);
                stats.OutstandingBalance = Convert.ToDecimal(row["OutstandingBalance"]);
            }

            return stats;
        }

        public static DataTable GetBestSellers(clsUser? actingUser, int top)
        {
            RequireAdmin(actingUser);
            return clsReportData.GetBestSellers(top);
        }

        // Adds a "Period" column like 2026-04 for the chart and the grid
        public static DataTable GetMonthlySales(clsUser? actingUser)
        {
            RequireAdmin(actingUser);

            DataTable table = clsReportData.GetMonthlySales();
            table.Columns.Add("Period", typeof(string));

            foreach (DataRow row in table.Rows)
                row["Period"] = Convert.ToInt32(row["SalesYear"]) + "-" + Convert.ToInt32(row["SalesMonth"]).ToString("00");

            return table;
        }

        public static DataTable GetSalesByCategory(clsUser? actingUser, DateTime from, DateTime to)
        {
            RequireAdmin(actingUser);

            if (from.Date > to.Date)
                throw new ArgumentException("The start date must not be after the end date.");

            return clsReportData.GetSalesByCategory(from, to);
        }
    }
}