/* =====================================================================
   ECMS - E-Commerce Management System
   Script 01: Create database + all tables, constraints and indexes
   ---------------------------------------------------------------------
   WARNING: This script DROPS and RE-CREATES the database ECMS_DB.
            Running it again erases all data in ECMS_DB (good for
            starting fresh while you are developing).
   Run order: 01_schema -> 02_seed -> 03_views -> 04_procedures
   ===================================================================== */

USE master;
GO

IF DB_ID(N'ECMS_DB') IS NOT NULL
BEGIN
    ALTER DATABASE ECMS_DB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ECMS_DB;
END
GO

CREATE DATABASE ECMS_DB;
GO

USE ECMS_DB;
GO

/* ---------------------------------------------------------------------
   1. People  (shared personal data for customers AND staff users)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.People
(
    PersonID    INT IDENTITY(1,1) NOT NULL,
    FirstName   NVARCHAR(50)  NOT NULL,
    LastName    NVARCHAR(50)  NOT NULL,
    Phone       NVARCHAR(20)  NOT NULL,
    Email       NVARCHAR(100) NULL,
    Address     NVARCHAR(250) NULL,
    DateOfBirth DATE          NULL,
    CONSTRAINT PK_People PRIMARY KEY (PersonID)
);
GO

-- Email must be unique, but many people may have no email (NULL).
CREATE UNIQUE INDEX UX_People_Email ON dbo.People (Email) WHERE Email IS NOT NULL;
CREATE INDEX IX_People_Name ON dbo.People (LastName, FirstName);
GO

/* ---------------------------------------------------------------------
   2. Users  (staff accounts that log in to the application)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Users
(
    UserID       INT IDENTITY(1,1) NOT NULL,
    PersonID     INT           NOT NULL,
    Username     NVARCHAR(50)  NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,          -- BCrypt hash, never the password
    Role         NVARCHAR(20)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Staff',
    IsActive     BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_PersonID UNIQUE (PersonID),     -- one account per person
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT FK_Users_People FOREIGN KEY (PersonID) REFERENCES dbo.People (PersonID),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Staff'))
);
GO

/* ---------------------------------------------------------------------
   3. Customers
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Customers
(
    CustomerID INT IDENTITY(1,1) NOT NULL,
    PersonID   INT          NOT NULL,
    CreatedAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    IsActive   BIT          NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT 1,
    CONSTRAINT PK_Customers PRIMARY KEY (CustomerID),
    CONSTRAINT UQ_Customers_PersonID UNIQUE (PersonID),
    CONSTRAINT FK_Customers_People FOREIGN KEY (PersonID) REFERENCES dbo.People (PersonID)
);
GO

/* ---------------------------------------------------------------------
   4. Categories
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Categories
(
    CategoryID  INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(50)  NOT NULL,
    Description NVARCHAR(250) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);
GO

/* ---------------------------------------------------------------------
   5. Products
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Products
(
    ProductID     INT IDENTITY(1,1) NOT NULL,
    Name          NVARCHAR(100)  NOT NULL,
    Description   NVARCHAR(1000) NULL,
    Price         DECIMAL(18,2)  NOT NULL,
    StockQuantity INT            NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0,
    ReorderLevel  INT            NOT NULL CONSTRAINT DF_Products_Reorder DEFAULT 5,
    CategoryID    INT            NOT NULL,
    IsActive      BIT            NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,   -- soft delete
    CreatedAt     DATETIME2(0)   NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Products PRIMARY KEY (ProductID),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories (CategoryID),
    CONSTRAINT CK_Products_Price   CHECK (Price >= 0),
    CONSTRAINT CK_Products_Stock   CHECK (StockQuantity >= 0),
    CONSTRAINT CK_Products_Reorder CHECK (ReorderLevel >= 0)
);
GO

CREATE INDEX IX_Products_CategoryID ON dbo.Products (CategoryID);
CREATE INDEX IX_Products_Name       ON dbo.Products (Name);
GO

/* ---------------------------------------------------------------------
   6. Orders
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Orders
(
    OrderID         INT IDENTITY(1,1) NOT NULL,
    CustomerID      INT           NOT NULL,
    CreatedByUserID INT           NOT NULL,       -- staff member who created the order
    OrderDate       DATETIME2(0)  NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSUTCDATETIME(),
    Status          NVARCHAR(20)  NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Pending',
    Subtotal        DECIMAL(18,2) NOT NULL,
    TaxAmount       DECIMAL(18,2) NOT NULL,
    TotalAmount     DECIMAL(18,2) NOT NULL,
    Notes           NVARCHAR(250) NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (OrderID),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerID)      REFERENCES dbo.Customers (CustomerID),
    CONSTRAINT FK_Orders_Users     FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users (UserID),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Pending', N'Processing', N'Shipped', N'Delivered', N'Cancelled')),
    CONSTRAINT CK_Orders_Amounts CHECK (Subtotal >= 0 AND TaxAmount >= 0 AND TotalAmount >= 0),
    CONSTRAINT CK_Orders_Total CHECK (TotalAmount = Subtotal + TaxAmount)
);
GO

CREATE INDEX IX_Orders_CustomerID_OrderDate ON dbo.Orders (CustomerID, OrderDate);
CREATE INDEX IX_Orders_Status               ON dbo.Orders (Status);
CREATE INDEX IX_Orders_CreatedByUserID      ON dbo.Orders (CreatedByUserID);
GO

/* ---------------------------------------------------------------------
   7. OrderItems  (junction table between Orders and Products)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.OrderItems
(
    OrderItemID INT IDENTITY(1,1) NOT NULL,
    OrderID     INT           NOT NULL,
    ProductID   INT           NOT NULL,
    Quantity    INT           NOT NULL,
    UnitPrice   DECIMAL(18,2) NOT NULL,           -- price at the time of sale
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderItemID),
    CONSTRAINT UQ_OrderItems_Order_Product UNIQUE (OrderID, ProductID),  -- one row per product per order
    CONSTRAINT FK_OrderItems_Orders   FOREIGN KEY (OrderID)   REFERENCES dbo.Orders (OrderID) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductID) REFERENCES dbo.Products (ProductID),
    CONSTRAINT CK_OrderItems_Quantity  CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0)
);
GO

CREATE INDEX IX_OrderItems_ProductID ON dbo.OrderItems (ProductID);
GO

/* ---------------------------------------------------------------------
   8. Payments  (an order can be paid in several parts)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Payments
(
    PaymentID        INT IDENTITY(1,1) NOT NULL,
    OrderID          INT           NOT NULL,
    Amount           DECIMAL(18,2) NOT NULL,
    Method           NVARCHAR(20)  NOT NULL,
    PaymentDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_Payments_Date DEFAULT SYSUTCDATETIME(),
    ReceivedByUserID INT           NOT NULL,
    CONSTRAINT PK_Payments PRIMARY KEY (PaymentID),
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderID)          REFERENCES dbo.Orders (OrderID),
    CONSTRAINT FK_Payments_Users  FOREIGN KEY (ReceivedByUserID) REFERENCES dbo.Users (UserID),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Method CHECK (Method IN (N'Cash', N'Card', N'Transfer'))
);
GO

CREATE INDEX IX_Payments_OrderID ON dbo.Payments (OrderID);
GO

/* ---------------------------------------------------------------------
   9. StockMovements  (history of every stock change)
      QuantityChange is signed: negative = stock out, positive = stock in
   --------------------------------------------------------------------- */
CREATE TABLE dbo.StockMovements
(
    MovementID     INT IDENTITY(1,1) NOT NULL,
    ProductID      INT           NOT NULL,
    QuantityChange INT           NOT NULL,
    MovementType   NVARCHAR(20)  NOT NULL,
    OrderID        INT           NULL,            -- NULL for manual adjustments / restocks
    UserID         INT           NOT NULL,
    MovementDate   DATETIME2(0)  NOT NULL CONSTRAINT DF_StockMovements_Date DEFAULT SYSUTCDATETIME(),
    Notes          NVARCHAR(250) NULL,
    CONSTRAINT PK_StockMovements PRIMARY KEY (MovementID),
    CONSTRAINT FK_StockMovements_Products FOREIGN KEY (ProductID) REFERENCES dbo.Products (ProductID),
    CONSTRAINT FK_StockMovements_Orders   FOREIGN KEY (OrderID)   REFERENCES dbo.Orders (OrderID),
    CONSTRAINT FK_StockMovements_Users    FOREIGN KEY (UserID)    REFERENCES dbo.Users (UserID),
    CONSTRAINT CK_StockMovements_Qty  CHECK (QuantityChange <> 0),
    CONSTRAINT CK_StockMovements_Type CHECK (MovementType IN (N'Sale', N'Cancellation', N'Adjustment', N'Restock'))
);
GO

CREATE INDEX IX_StockMovements_Product_Date ON dbo.StockMovements (ProductID, MovementDate);
CREATE INDEX IX_StockMovements_OrderID      ON dbo.StockMovements (OrderID);
GO

PRINT 'ECMS_DB schema created successfully (9 tables).';
GO
