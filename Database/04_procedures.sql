/* =====================================================================
   ECMS - Script 04: Stored procedures
   Safe to run more than once (CREATE OR ALTER).
   ===================================================================== */

USE ECMS_DB;
GO

/* Sales (revenue) per category between two dates */
CREATE OR ALTER PROCEDURE dbo.sp_SalesByCategory
    @From DATE,
    @To   DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.Name                           AS Category,
           SUM(oi.Quantity)                 AS UnitsSold,
           SUM(oi.Quantity * oi.UnitPrice)  AS Revenue
    FROM dbo.OrderItems oi
    JOIN dbo.Orders o     ON o.OrderID    = oi.OrderID
    JOIN dbo.Products p   ON p.ProductID  = oi.ProductID
    JOIN dbo.Categories c ON c.CategoryID = p.CategoryID
    WHERE o.OrderDate >= @From
      AND o.OrderDate <  DATEADD(DAY, 1, @To)
      AND o.Status <> N'Cancelled'
    GROUP BY c.Name
    ORDER BY Revenue DESC;
END;
GO

/* All orders of one customer */
CREATE OR ALTER PROCEDURE dbo.sp_GetCustomerOrders
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT OrderID, OrderDate, Status, TotalAmount, PaidAmount, RemainingAmount
    FROM dbo.vw_OrderSummary
    WHERE CustomerID = @CustomerID
    ORDER BY OrderDate DESC;
END;
GO

/* Items of one order (used by the Order Details form) */
CREATE OR ALTER PROCEDURE dbo.sp_GetOrderItems
    @OrderID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT oi.OrderItemID,
           oi.ProductID,
           p.Name                       AS ProductName,
           oi.Quantity,
           oi.UnitPrice,
           oi.Quantity * oi.UnitPrice   AS LineTotal
    FROM dbo.OrderItems oi
    JOIN dbo.Products p ON p.ProductID = oi.ProductID
    WHERE oi.OrderID = @OrderID
    ORDER BY oi.OrderItemID;
END;
GO

PRINT 'Stored procedures created successfully (3 procedures).';
GO
