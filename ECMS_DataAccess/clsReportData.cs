using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ECMS_DataAccess
{
    // All the report queries. They use the views and the stored procedure created in Stage 1.
    public static class clsReportData
    {
        // One row with the numbers shown on the dashboard
        public static DataTable GetDashboardStats()
        {
            string sql = @"SELECT
                               (SELECT COUNT(*) FROM dbo.Customers WHERE IsActive = 1)  AS ActiveCustomers,
                               (SELECT COUNT(*) FROM dbo.Products  WHERE IsActive = 1)  AS ActiveProducts,
                               (SELECT COUNT(*) FROM dbo.Orders)                        AS TotalOrders,
                               (SELECT COUNT(*) FROM dbo.Orders WHERE Status = N'Pending') AS PendingOrders,
                               (SELECT COUNT(*) FROM dbo.vw_LowStock)                   AS LowStockProducts,
                               (SELECT ISNULL(SUM(TotalAmount), 0)
                                FROM dbo.Orders WHERE Status <> N'Cancelled')           AS Revenue,
                               (SELECT ISNULL(SUM(RemainingAmount), 0)
                                FROM dbo.vw_OrderSummary WHERE Status <> N'Cancelled')  AS OutstandingBalance;";

            return clsDbHelper.GetTable(sql);
        }

        // Uses the view vw_BestSellingProducts
        public static DataTable GetBestSellers(int top)
        {
            string sql = @"SELECT TOP (@Top) ProductID, Name, Category, UnitsSold, Revenue
                           FROM dbo.vw_BestSellingProducts
                           ORDER BY UnitsSold DESC, Revenue DESC;";

            return clsDbHelper.GetTable(sql, cmd =>
                cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top);
        }

        // Uses the view vw_MonthlySales
        public static DataTable GetMonthlySales()
        {
            string sql = @"SELECT SalesYear, SalesMonth, OrdersCount, Revenue
                           FROM dbo.vw_MonthlySales
                           ORDER BY SalesYear, SalesMonth;";

            return clsDbHelper.GetTable(sql);
        }

        // Uses the stored procedure sp_SalesByCategory
        public static DataTable GetSalesByCategory(DateTime from, DateTime to)
        {
            return clsDbHelper.GetTable("dbo.sp_SalesByCategory", cmd =>
            {
                cmd.Parameters.Add("@From", SqlDbType.Date).Value = from.Date;
                cmd.Parameters.Add("@To", SqlDbType.Date).Value = to.Date;
            }, CommandType.StoredProcedure);
        }
    }
}