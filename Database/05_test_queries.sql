/* =====================================================================
   ECMS - Script 05: Checks and practice queries
   Nothing here changes your data (the constraint tests are rolled back).
   Run the sections one at a time: select the lines, then press F5.
   ===================================================================== */

USE ECMS_DB;
GO

/* ---------------------------------------------------------------------
   SECTION A - Did everything load? Expected row counts:
   People 13 | Users 3 | Customers 10 | Categories 5 | Products 20
   Orders 16 | OrderItems 41 | Payments 12 | StockMovements 64
   --------------------------------------------------------------------- */
SELECT 'People' AS TableName, COUNT(*) AS Rows FROM dbo.People
UNION ALL SELECT 'Users',           COUNT(*) FROM dbo.Users
UNION ALL SELECT 'Customers',       COUNT(*) FROM dbo.Customers
UNION ALL SELECT 'Categories',      COUNT(*) FROM dbo.Categories
UNION ALL SELECT 'Products',        COUNT(*) FROM dbo.Products
UNION ALL SELECT 'Orders',          COUNT(*) FROM dbo.Orders
UNION ALL SELECT 'OrderItems',      COUNT(*) FROM dbo.OrderItems
UNION ALL SELECT 'Payments',        COUNT(*) FROM dbo.Payments
UNION ALL SELECT 'StockMovements',  COUNT(*) FROM dbo.StockMovements;
GO

/* ---------------------------------------------------------------------
   SECTION B - Data consistency checks (each query should return 0 rows)
   --------------------------------------------------------------------- */
-- B1. Product stock must equal the sum of its stock movements
SELECT p.ProductID, p.Name, p.StockQuantity, m.TotalChange
FROM dbo.Products p
JOIN (SELECT ProductID, SUM(QuantityChange) AS TotalChange
      FROM dbo.StockMovements GROUP BY ProductID) m ON m.ProductID = p.ProductID
WHERE p.StockQuantity <> m.TotalChange;

-- B2. Order subtotal must equal the sum of its items
SELECT o.OrderID, o.Subtotal, s.ItemsTotal
FROM dbo.Orders o
JOIN (SELECT OrderID, SUM(Quantity * UnitPrice) AS ItemsTotal
      FROM dbo.OrderItems GROUP BY OrderID) s ON s.OrderID = o.OrderID
WHERE o.Subtotal <> s.ItemsTotal;

-- B3. No order may be paid more than its total
SELECT OrderID, TotalAmount, PaidAmount
FROM dbo.vw_OrderSummary
WHERE PaidAmount > TotalAmount;
GO

/* ---------------------------------------------------------------------
   SECTION C - Constraint tests (everything is rolled back at the end).
   Every line should print PASS.
   --------------------------------------------------------------------- */
BEGIN TRANSACTION;

BEGIN TRY
    INSERT INTO dbo.Products (Name, Price, StockQuantity, CategoryID) VALUES (N'Bad', -5, 1, 1);
    PRINT 'FAIL: negative price was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: negative price rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.Products (Name, Price, StockQuantity, CategoryID) VALUES (N'Bad', 5, -1, 1);
    PRINT 'FAIL: negative stock was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: negative stock rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.Orders (CustomerID, CreatedByUserID, Status, Subtotal, TaxAmount, TotalAmount)
    VALUES (1, 1, N'Unknown', 0, 0, 0);
    PRINT 'FAIL: invalid order status was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: invalid order status rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.Orders (CustomerID, CreatedByUserID, Subtotal, TaxAmount, TotalAmount)
    VALUES (1, 1, 100, 15, 999);
    PRINT 'FAIL: wrong order total was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: total <> subtotal + tax rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity, UnitPrice) VALUES (1, 2, 0, 10);
    PRINT 'FAIL: quantity 0 was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: quantity 0 rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity, UnitPrice) VALUES (1, 1, 1, 10);
    PRINT 'FAIL: same product twice in one order was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: duplicate product in the same order rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.Users (PersonID, Username, PasswordHash, Role) VALUES (4, N'admin', N'x', N'Staff');
    PRINT 'FAIL: duplicate username was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: duplicate username rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.Users (PersonID, Username, PasswordHash, Role) VALUES (4, N'newuser', N'x', N'Boss');
    PRINT 'FAIL: invalid role was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: invalid role rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.People (FirstName, LastName, Phone, Email)
    VALUES (N'Copy', N'Cat', N'0500000000', N'ahmed.hassan@example.com');
    PRINT 'FAIL: duplicate email was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: duplicate email rejected';
END CATCH

BEGIN TRY
    INSERT INTO dbo.Payments (OrderID, Amount, Method, ReceivedByUserID) VALUES (1, 0, N'Cash', 1);
    PRINT 'FAIL: payment of 0 was accepted';
END TRY
BEGIN CATCH
    PRINT 'PASS: payment of 0 rejected';
END CATCH

BEGIN TRY
    DELETE FROM dbo.Products WHERE ProductID = 1;
    PRINT 'FAIL: a product used in orders was deleted';
END TRY
BEGIN CATCH
    PRINT 'PASS: product used in orders cannot be deleted (use IsActive = 0)';
END CATCH

ROLLBACK TRANSACTION;
GO

/* ---------------------------------------------------------------------
   SECTION D - Practice queries (your SQL showcase)
   --------------------------------------------------------------------- */

-- D1. INNER JOIN: products with their category
SELECT p.Name, p.Price, p.StockQuantity, c.Name AS Category
FROM dbo.Products p
INNER JOIN dbo.Categories c ON c.CategoryID = p.CategoryID
ORDER BY c.Name, p.Name;

-- D2. LEFT JOIN + GROUP BY: number of products per category
SELECT c.Name, COUNT(p.ProductID) AS ProductCount
FROM dbo.Categories c
LEFT JOIN dbo.Products p ON p.CategoryID = c.CategoryID
GROUP BY c.Name;

-- D3. HAVING: categories that have more than 3 products
SELECT c.Name, COUNT(p.ProductID) AS ProductCount
FROM dbo.Categories c
LEFT JOIN dbo.Products p ON p.CategoryID = c.CategoryID
GROUP BY c.Name
HAVING COUNT(p.ProductID) > 3;

-- D4. Total revenue (cancelled orders excluded)
SELECT SUM(TotalAmount) AS TotalRevenue
FROM dbo.Orders
WHERE Status <> N'Cancelled';

-- D5. Orders with customer name, paid and remaining (uses the view)
SELECT * FROM dbo.vw_OrderSummary ORDER BY OrderDate DESC;

-- D6. Orders that are not fully paid
SELECT OrderID, CustomerName, Status, TotalAmount, PaidAmount, RemainingAmount
FROM dbo.vw_OrderSummary
WHERE RemainingAmount > 0 AND Status <> N'Cancelled'
ORDER BY RemainingAmount DESC;

-- D7. Best-selling products (top 5)
SELECT TOP 5 * FROM dbo.vw_BestSellingProducts ORDER BY UnitsSold DESC;

-- D8. Monthly sales
SELECT * FROM dbo.vw_MonthlySales ORDER BY SalesYear, SalesMonth;

-- D9. Low-stock products
SELECT * FROM dbo.vw_LowStock ORDER BY StockQuantity;

-- D10. Customers ranked by total spending
SELECT pe.FirstName + N' ' + pe.LastName AS Customer,
       COUNT(o.OrderID)  AS Orders,
       SUM(o.TotalAmount) AS TotalSpent
FROM dbo.Customers cu
JOIN dbo.People pe ON pe.PersonID = cu.PersonID
JOIN dbo.Orders o  ON o.CustomerID = cu.CustomerID
WHERE o.Status <> N'Cancelled'
GROUP BY pe.FirstName, pe.LastName
ORDER BY TotalSpent DESC;

-- D11. Stock history of one product (Gaming Laptop)
SELECT sm.MovementDate, sm.MovementType, sm.QuantityChange, sm.OrderID, u.Username, sm.Notes
FROM dbo.StockMovements sm
JOIN dbo.Users u ON u.UserID = sm.UserID
WHERE sm.ProductID = 1
ORDER BY sm.MovementDate;

-- D12. Stored procedures
EXEC dbo.sp_SalesByCategory @From = '2026-04-01', @To = '2026-10-31';
EXEC dbo.sp_GetCustomerOrders @CustomerID = 2;
EXEC dbo.sp_GetOrderItems @OrderID = 1;
GO
