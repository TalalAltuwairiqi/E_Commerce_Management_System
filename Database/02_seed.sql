/* =====================================================================
   ECMS - Script 02: Sample (seed) data
   ---------------------------------------------------------------------
   Run AFTER 01_schema.sql, once, on an empty database.
   If the database already contains data, this script does nothing.
   (To start over: run 01_schema.sql again, then this script.)

   Demo logins (passwords are stored as BCrypt hashes):
       admin   /  Admin@123    (role: Admin)
       lina    /  Staff@123    (role: Staff)
       yousef  /  Staff@123    (role: Staff)

   Stock numbers are NOT typed by hand: they are calculated from the
   StockMovements history at the end, so stock and history always agree.
   ===================================================================== */

USE ECMS_DB;
GO
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM dbo.People)
    PRINT 'Seed skipped: ECMS_DB already contains data. Run 01_schema.sql first to start fresh.'
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        /* ---------- People: 3 staff (IDs 1-3) + 10 customers (IDs 4-13) ---------- */
        INSERT INTO dbo.People (FirstName, LastName, Phone, Email, Address, DateOfBirth) VALUES
        (N'Omar',    N'Khalid',    N'0501000001', N'omar.khalid@ecms.com',     N'Jeddah',  '1988-03-14'),
        (N'Lina',    N'Nasser',    N'0501000002', N'lina.nasser@ecms.com',     N'Jeddah',  '1995-07-22'),
        (N'Yousef',  N'Barakat',   N'0501000003', N'yousef.barakat@ecms.com',  N'Makkah',  '1997-11-05'),
        (N'Ahmed',   N'Hassan',    N'0551000004', N'ahmed.hassan@example.com',   N'Jeddah, Al Rawdah',   '1990-01-18'),
        (N'Fatima',  N'Zahra',     N'0551000005', N'fatima.zahra@example.com',   N'Riyadh, Olaya',       '1993-05-30'),
        (N'Khalid',  N'Mansour',   N'0551000006', N'khalid.mansour@example.com', N'Dammam',              '1985-09-12'),
        (N'Noura',   N'Alqahtani', N'0551000007', N'noura.alqahtani@example.com',N'Jeddah, Al Safa',     '1998-02-27'),
        (N'Faisal',  N'Otaibi',    N'0551000008', N'faisal.otaibi@example.com',  N'Riyadh, Al Malaz',    '1987-12-03'),
        (N'Mariam',  N'Saleh',     N'0551000009', NULL,                          N'Madinah',             '1996-06-09'),
        (N'Hassan',  N'Ali',       N'0551000010', N'hassan.ali@example.com',     N'Jeddah, Al Hamra',    '1991-08-21'),
        (N'Reem',    N'Abdullah',  N'0551000011', N'reem.abdullah@example.com',  N'Taif',                '1999-04-15'),
        (N'Tariq',   N'Younes',    N'0551000012', NULL,                          N'Khobar',              '1983-10-28'),
        (N'Salma',   N'Ibrahim',   N'0551000013', N'salma.ibrahim@example.com',  N'Jeddah, Al Naeem',    '2000-01-07');

        /* ---------- Users (BCrypt hashes of Admin@123 and Staff@123) ---------- */
        INSERT INTO dbo.Users (PersonID, Username, PasswordHash, Role) VALUES
        (1, N'admin',  N'$2b$11$JsDtZPxHqrG27tfCoQbj1u2E/rJwGiyOzRPllxxYsd9b8JyidUuiu', N'Admin'),
        (2, N'lina',   N'$2b$11$taKFikmGv7hPrwLvcS5aFeXJWFKqNz6gsEIAaMwbvTHYBFVqE8YPe', N'Staff'),
        (3, N'yousef', N'$2b$11$taKFikmGv7hPrwLvcS5aFeXJWFKqNz6gsEIAaMwbvTHYBFVqE8YPe', N'Staff');

        /* ---------- Customers: People 4-13 become Customers 1-10 ---------- */
        INSERT INTO dbo.Customers (PersonID, CreatedAt)
        SELECT PersonID, '2026-03-01' FROM dbo.People WHERE PersonID BETWEEN 4 AND 13 ORDER BY PersonID;

        /* ---------- Categories ---------- */
        INSERT INTO dbo.Categories (Name, Description) VALUES
        (N'Computers',   N'Laptops, desktops and tablets'),
        (N'Phones',      N'Smartphones and phone accessories'),
        (N'Accessories', N'Keyboards, mice, bags and other accessories'),
        (N'Monitors',    N'Monitors and monitor accessories'),
        (N'Networking',  N'Routers, switches and cables');

        /* ---------- Products (stock starts at 0, filled from StockMovements below) ---------- */
        INSERT INTO dbo.Products (Name, Description, Price, StockQuantity, ReorderLevel, CategoryID) VALUES
        (N'Gaming Laptop',        N'15.6" gaming laptop, 16GB RAM, 1TB SSD',  1200.00, 0, 5, 1),  -- 1
        (N'Office Laptop',        N'14" laptop for office work, 8GB RAM',      650.00, 0, 5, 1),  -- 2
        (N'Mini Desktop PC',      N'Compact desktop computer',                 480.00, 0, 3, 1),  -- 3
        (N'Tablet 10-inch',       N'10-inch tablet, 128GB',                    320.00, 0, 5, 1),  -- 4
        (N'Smartphone Pro',       N'Flagship smartphone, 256GB',               900.00, 0, 5, 2),  -- 5
        (N'Smartphone Lite',      N'Budget smartphone, 128GB',                 350.00, 0, 5, 2),  -- 6
        (N'Phone Case',           N'Protective phone case',                     15.00, 0, 20, 2), -- 7
        (N'Wireless Earbuds',     N'Bluetooth earbuds with charging case',      80.00, 0, 10, 2), -- 8
        (N'Mechanical Keyboard',  N'RGB mechanical keyboard',                  100.00, 0, 5, 3),  -- 9
        (N'Wireless Mouse',       N'Ergonomic wireless mouse',                  25.00, 0, 10, 3), -- 10
        (N'USB-C Hub',            N'7-in-1 USB-C hub',                          45.00, 0, 5, 3),  -- 11
        (N'Laptop Bag',           N'15.6" water-resistant laptop bag',          40.00, 0, 5, 3),  -- 12
        (N'Webcam HD',            N'1080p webcam with microphone',              55.00, 0, 5, 3),  -- 13
        (N'24-inch Monitor',      N'24" Full HD IPS monitor',                  150.00, 0, 5, 4),  -- 14
        (N'27-inch 4K Monitor',   N'27" 4K UHD monitor',                       380.00, 0, 3, 4),  -- 15
        (N'Monitor Stand',        N'Adjustable monitor stand',                  30.00, 0, 5, 4),  -- 16
        (N'WiFi Router',          N'Dual-band WiFi 6 router',                   70.00, 0, 5, 5),  -- 17
        (N'Network Switch 8-port',N'Gigabit 8-port switch',                     35.00, 0, 5, 5),  -- 18
        (N'Ethernet Cable 3m',    N'Cat6 Ethernet cable, 3 meters',              6.00, 0, 30, 5), -- 19
        (N'WiFi Range Extender',  N'Dual-band range extender',                  45.00, 0, 5, 5);  -- 20

        /* ---------- Initial stock: one Restock movement per product ---------- */
        INSERT INTO dbo.StockMovements (ProductID, QuantityChange, MovementType, OrderID, UserID, MovementDate, Notes) VALUES
        (1, 17,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (2, 28,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (3, 11,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (4, 19,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (5, 22,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (6, 33,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (7, 106, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (8, 44,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (9, 40,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (10, 66, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (11, 10, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (12, 25, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (13, 7,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (14, 16, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (15, 9,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (16, 4,  N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (17, 26, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (18, 16, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (19, 165,N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock'),
        (20, 14, N'Restock', NULL, 1, '2026-03-25T09:00:00', N'Initial stock');

        /* ---------- Orders (IDs 1-16). Totals are filled in after the items are inserted ---------- */
        INSERT INTO dbo.Orders (CustomerID, CreatedByUserID, OrderDate, Status, Subtotal, TaxAmount, TotalAmount, Notes) VALUES
        (1,  2, '2026-04-03T10:15:00', N'Delivered',  0, 0, 0, NULL),                        -- 1
        (2,  2, '2026-04-12T14:30:00', N'Delivered',  0, 0, 0, NULL),                        -- 2
        (3,  3, '2026-04-25T11:05:00', N'Delivered',  0, 0, 0, N'Office purchase'),          -- 3
        (4,  2, '2026-05-06T16:20:00', N'Delivered',  0, 0, 0, NULL),                        -- 4
        (5,  3, '2026-05-19T12:45:00', N'Delivered',  0, 0, 0, NULL),                        -- 5
        (1,  2, '2026-05-28T09:50:00', N'Cancelled',  0, 0, 0, N'Customer changed his mind'), -- 6
        (6,  3, '2026-06-09T13:10:00', N'Delivered',  0, 0, 0, NULL),                        -- 7
        (7,  2, '2026-06-21T15:40:00', N'Delivered',  0, 0, 0, NULL),                        -- 8
        (8,  3, '2026-07-04T10:00:00', N'Delivered',  0, 0, 0, NULL),                        -- 9
        (2,  2, '2026-07-18T17:25:00', N'Shipped',    0, 0, 0, NULL),                        -- 10
        (9,  3, '2026-08-02T11:35:00', N'Delivered',  0, 0, 0, NULL),                        -- 11
        (10, 2, '2026-08-15T14:05:00', N'Shipped',    0, 0, 0, NULL),                        -- 12
        (3,  3, '2026-09-01T10:30:00', N'Processing', 0, 0, 0, N'Deposit paid'),             -- 13
        (5,  2, '2026-09-12T12:15:00', N'Processing', 0, 0, 0, NULL),                        -- 14
        (6,  3, '2026-09-28T16:45:00', N'Pending',    0, 0, 0, NULL),                        -- 15
        (4,  2, '2026-10-02T09:20:00', N'Pending',    0, 0, 0, NULL);                        -- 16

        /* ---------- Order items: unit price is copied from the product's current price ---------- */
        INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity, UnitPrice)
        SELECT v.OrderID, v.ProductID, v.Quantity, p.Price
        FROM (VALUES
            (1, 1, 1), (1, 9, 1), (1, 10, 1),
            (2, 5, 1), (2, 7, 2), (2, 8, 1),
            (3, 2, 2), (3, 12, 2), (3, 10, 2),
            (4, 14, 2), (4, 16, 2), (4, 19, 5),
            (5, 6, 1), (5, 7, 1),
            (6, 1, 1), (6, 15, 1),
            (7, 17, 1), (7, 18, 2), (7, 19, 10),
            (8, 4, 1), (8, 8, 2),
            (9, 3, 1), (9, 14, 1), (9, 9, 1),
            (10, 15, 1), (10, 11, 2),
            (11, 2, 1), (11, 13, 2),
            (12, 5, 1), (12, 8, 1), (12, 12, 1),
            (13, 9, 3), (13, 10, 3), (13, 11, 2),
            (14, 1, 1), (14, 14, 1),
            (15, 6, 2), (15, 7, 3),
            (16, 13, 1), (16, 20, 2), (16, 11, 1)
        ) AS v (OrderID, ProductID, Quantity)
        JOIN dbo.Products p ON p.ProductID = v.ProductID;

        /* ---------- Calculate order totals: tax = 15% of subtotal ---------- */
        UPDATE o
        SET Subtotal    = s.Sub,
            TaxAmount   = ROUND(s.Sub * 0.15, 2),
            TotalAmount = s.Sub + ROUND(s.Sub * 0.15, 2)
        FROM dbo.Orders o
        JOIN (SELECT OrderID, SUM(Quantity * UnitPrice) AS Sub
              FROM dbo.OrderItems GROUP BY OrderID) s ON s.OrderID = o.OrderID;

        /* ---------- Stock history: every order item is a Sale ---------- */
        INSERT INTO dbo.StockMovements (ProductID, QuantityChange, MovementType, OrderID, UserID, MovementDate, Notes)
        SELECT oi.ProductID, -oi.Quantity, N'Sale', oi.OrderID, o.CreatedByUserID, o.OrderDate, N'Order sale'
        FROM dbo.OrderItems oi
        JOIN dbo.Orders o ON o.OrderID = oi.OrderID;

        -- Cancelled orders return their stock the next day
        INSERT INTO dbo.StockMovements (ProductID, QuantityChange, MovementType, OrderID, UserID, MovementDate, Notes)
        SELECT oi.ProductID, oi.Quantity, N'Cancellation', oi.OrderID, 1, DATEADD(DAY, 1, o.OrderDate), N'Order cancelled'
        FROM dbo.OrderItems oi
        JOIN dbo.Orders o ON o.OrderID = oi.OrderID
        WHERE o.Status = N'Cancelled';

        -- One manual adjustment (damaged goods)
        INSERT INTO dbo.StockMovements (ProductID, QuantityChange, MovementType, OrderID, UserID, MovementDate, Notes)
        VALUES (7, -2, N'Adjustment', NULL, 1, '2026-06-30T17:00:00', N'Damaged phone cases');

        /* ---------- Current stock = sum of all movements ---------- */
        UPDATE p
        SET StockQuantity = m.TotalChange
        FROM dbo.Products p
        JOIN (SELECT ProductID, SUM(QuantityChange) AS TotalChange
              FROM dbo.StockMovements GROUP BY ProductID) m ON m.ProductID = p.ProductID;

        /* ---------- Payments ---------- */
        -- Delivered orders: paid in full (method varies)
        INSERT INTO dbo.Payments (OrderID, Amount, Method, PaymentDate, ReceivedByUserID)
        SELECT o.OrderID, o.TotalAmount,
               CASE o.OrderID % 3 WHEN 0 THEN N'Cash' WHEN 1 THEN N'Card' ELSE N'Transfer' END,
               DATEADD(HOUR, 1, o.OrderDate), o.CreatedByUserID
        FROM dbo.Orders o
        WHERE o.Status = N'Delivered';

        -- Order 10 (shipped): paid in full
        INSERT INTO dbo.Payments (OrderID, Amount, Method, PaymentDate, ReceivedByUserID)
        SELECT OrderID, TotalAmount, N'Card', DATEADD(HOUR, 1, OrderDate), CreatedByUserID
        FROM dbo.Orders WHERE OrderID = 10;

        -- Order 12 (shipped): 50% paid
        INSERT INTO dbo.Payments (OrderID, Amount, Method, PaymentDate, ReceivedByUserID)
        SELECT OrderID, ROUND(TotalAmount * 0.5, 2), N'Cash', DATEADD(HOUR, 1, OrderDate), CreatedByUserID
        FROM dbo.Orders WHERE OrderID = 12;

        -- Order 13 (processing): 40% deposit
        INSERT INTO dbo.Payments (OrderID, Amount, Method, PaymentDate, ReceivedByUserID)
        SELECT OrderID, ROUND(TotalAmount * 0.4, 2), N'Transfer', DATEADD(HOUR, 1, OrderDate), CreatedByUserID
        FROM dbo.Orders WHERE OrderID = 13;

        COMMIT TRANSACTION;
        PRINT 'Seed data inserted successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
