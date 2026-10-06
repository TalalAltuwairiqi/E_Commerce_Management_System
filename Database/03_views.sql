/* =====================================================================
   ECMS - Script 03: Views (ready-made queries used by forms and reports)
   Safe to run more than once (CREATE OR ALTER).
   ===================================================================== */

USE ECMS_DB;
GO

/* Orders with customer name, staff member, paid and remaining amounts */
CREATE OR ALTER VIEW dbo.vw_OrderSummary
AS
SELECT o.OrderID,
       o.CustomerID,
       pe.FirstName + N' ' + pe.LastName            AS CustomerName,
       u.Username                                   AS CreatedBy,
       o.OrderDate,
       o.Status,
       o.Subtotal,
       o.TaxAmount,
       o.TotalAmount,
       ISNULL(pay.PaidAmount, 0)                    AS PaidAmount,
       o.TotalAmount - ISNULL(pay.PaidAmount, 0)    AS RemainingAmount
FROM dbo.Orders o
JOIN dbo.Customers cu ON cu.CustomerID = o.CustomerID
JOIN dbo.People pe    ON pe.PersonID   = cu.PersonID
JOIN dbo.Users u      ON u.UserID      = o.CreatedByUserID
LEFT JOIN (SELECT OrderID, SUM(Amount) AS PaidAmount
           FROM dbo.Payments
           GROUP BY OrderID) pay ON pay.OrderID = o.OrderID;
GO

/* Best-selling products (cancelled orders are ignored) */
CREATE OR ALTER VIEW dbo.vw_BestSellingProducts
AS
SELECT p.ProductID,
       p.Name,
       c.Name                           AS Category,
       SUM(oi.Quantity)                 AS UnitsSold,
       SUM(oi.Quantity * oi.UnitPrice)  AS Revenue
FROM dbo.OrderItems oi
JOIN dbo.Orders o     ON o.OrderID    = oi.OrderID
JOIN dbo.Products p   ON p.ProductID  = oi.ProductID
JOIN dbo.Categories c ON c.CategoryID = p.CategoryID
WHERE o.Status <> N'Cancelled'
GROUP BY p.ProductID, p.Name, c.Name;
GO

/* Products at or below their reorder level */
CREATE OR ALTER VIEW dbo.vw_LowStock
AS
SELECT p.ProductID,
       p.Name,
       c.Name AS Category,
       p.StockQuantity,
       p.ReorderLevel
FROM dbo.Products p
JOIN dbo.Categories c ON c.CategoryID = p.CategoryID
WHERE p.IsActive = 1
  AND p.StockQuantity <= p.ReorderLevel;
GO

/* Monthly sales (cancelled orders are ignored) */
CREATE OR ALTER VIEW dbo.vw_MonthlySales
AS
SELECT YEAR(OrderDate)   AS SalesYear,
       MONTH(OrderDate)  AS SalesMonth,
       COUNT(*)          AS OrdersCount,
       SUM(TotalAmount)  AS Revenue
FROM dbo.Orders
WHERE Status <> N'Cancelled'
GROUP BY YEAR(OrderDate), MONTH(OrderDate);
GO

PRINT 'Views created successfully (4 views).';
GO
