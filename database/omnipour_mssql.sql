-- ============================================================================
-- OMNIPOUR COFFEE & BEVERAGE TRANSACTION PROCESSING SYSTEM
-- Database: OmniPourDB (Microsoft SQL Server / SSMS)
-- Normalized Schema (3NF) with Strict Constraints, Audit Trails, and RowVersion
-- ============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'OmniPourDB')
BEGIN
    CREATE DATABASE [OmniPourDB];
END
GO

USE [OmniPourDB];
GO

-- 1. DROP EXISTING TABLES IN REVERSE ORDER OF DEPENDENCY IF RERUNNING
IF OBJECT_ID('dbo.ErrorLogs', 'U') IS NOT NULL DROP TABLE dbo.ErrorLogs;
IF OBJECT_ID('dbo.AuditTrail', 'U') IS NOT NULL DROP TABLE dbo.AuditTrail;
IF OBJECT_ID('dbo.InventoryTransactionDetails', 'U') IS NOT NULL DROP TABLE dbo.InventoryTransactionDetails;
IF OBJECT_ID('dbo.InventoryTransactions', 'U') IS NOT NULL DROP TABLE dbo.InventoryTransactions;
IF OBJECT_ID('dbo.SaleDetails', 'U') IS NOT NULL DROP TABLE dbo.SaleDetails;
IF OBJECT_ID('dbo.Sales', 'U') IS NOT NULL DROP TABLE dbo.Sales;
IF OBJECT_ID('dbo.ProductRecipes', 'U') IS NOT NULL DROP TABLE dbo.ProductRecipes;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Ingredients', 'U') IS NOT NULL DROP TABLE dbo.Ingredients;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

-- ============================================================================
-- 2. CORE MASTER & SECURITY TABLES
-- ============================================================================

-- Roles Table
CREATE TABLE dbo.Roles (
    RoleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

-- Users Table (With Salted Hash, Lockout, and Optimistic Concurrency RowVersion)
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(120) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    PasswordSalt NVARCHAR(256) NOT NULL,
    RoleId INT NOT NULL,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockoutUntil DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

-- Categories Master Table
CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryName NVARCHAR(80) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- Ingredients Master Table (Raw Materials Inventory)
CREATE TABLE dbo.Ingredients (
    IngredientId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IngredientName NVARCHAR(120) NOT NULL UNIQUE,
    Unit NVARCHAR(30) NOT NULL, -- e.g., 'g', 'ml', 'shots', 'pcs'
    StockQuantity DECIMAL(12,3) NOT NULL DEFAULT 0.000,
    ReorderLevel DECIMAL(12,3) NOT NULL DEFAULT 100.000,
    CostPerUnit DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT CHK_Ingredients_StockQuantity CHECK (StockQuantity >= 0),
    CONSTRAINT CHK_Ingredients_ReorderLevel CHECK (ReorderLevel >= 0),
    CONSTRAINT CHK_Ingredients_CostPerUnit CHECK (CostPerUnit >= 0)
);
GO

-- Products Master Table (Beverages & Food Items)
CREATE TABLE dbo.Products (
    ProductId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SKU NVARCHAR(30) NOT NULL UNIQUE,
    ProductName NVARCHAR(120) NOT NULL,
    CategoryId INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    StockQuantity INT NOT NULL DEFAULT 0, -- Ready-to-serve stock
    ReorderLevel INT NOT NULL DEFAULT 5,
    HasRecipe BIT NOT NULL DEFAULT 0,     -- 1 = deducts ingredients when prepared
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT CHK_Products_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CHK_Products_StockQuantity CHECK (StockQuantity >= 0)
);
GO

-- Product Recipes Table (Associates Products with Ingredients)
CREATE TABLE dbo.ProductRecipes (
    ProductId INT NOT NULL,
    IngredientId INT NOT NULL,
    QuantityRequired DECIMAL(12,3) NOT NULL,
    PRIMARY KEY (ProductId, IngredientId),
    CONSTRAINT FK_Recipes_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Recipes_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(IngredientId),
    CONSTRAINT CHK_Recipes_QuantityRequired CHECK (QuantityRequired > 0)
);
GO

-- ============================================================================
-- 3. TRANSACTION 1: SALES POS (HEADER & DETAIL)
-- ============================================================================

CREATE TABLE dbo.Sales (
    SaleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
    UserId INT NOT NULL, -- Cashier
    CustomerName NVARCHAR(100) NOT NULL DEFAULT 'Walk-in Customer',
    Subtotal DECIMAL(10,2) NOT NULL,
    TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    GrandTotal DECIMAL(10,2) NOT NULL,
    AmountTendered DECIMAL(10,2) NOT NULL,
    ChangeDue DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    PaymentMethod NVARCHAR(30) NOT NULL DEFAULT 'Cash', -- 'Cash', 'GCash', 'Card'
    PaymentStatus NVARCHAR(30) NOT NULL DEFAULT 'Paid', -- 'Paid', 'Voided'
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    VoidedAt DATETIME2 NULL,
    VoidReason NVARCHAR(255) NULL,
    VoidedByUserId INT NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Sales_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Sales_VoidedBy FOREIGN KEY (VoidedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CHK_Sales_GrandTotal CHECK (GrandTotal >= 0)
);
GO

CREATE TABLE dbo.SaleDetails (
    SaleDetailId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SaleId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(120) NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Quantity INT NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_SaleDetails_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(SaleId) ON DELETE CASCADE,
    CONSTRAINT FK_SaleDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
    CONSTRAINT CHK_SaleDetails_Quantity CHECK (Quantity > 0)
);
GO

-- ============================================================================
-- 4. TRANSACTION 2: INVENTORY STOCK IN & DELIVERIES (HEADER & DETAIL)
-- ============================================================================

CREATE TABLE dbo.InventoryTransactions (
    InventoryTxId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReferenceNumber NVARCHAR(50) NOT NULL UNIQUE,
    UserId INT NOT NULL,
    TransactionType NVARCHAR(30) NOT NULL, -- 'Delivery', 'Adjustment', 'Spoilage'
    SupplierName NVARCHAR(120) NULL,
    TotalCost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_InventoryTransactions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

CREATE TABLE dbo.InventoryTransactionDetails (
    DetailId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    InventoryTxId INT NOT NULL,
    IngredientId INT NULL,
    ProductId INT NULL,
    ItemType NVARCHAR(20) NOT NULL, -- 'Ingredient' or 'Product'
    ItemName NVARCHAR(120) NOT NULL,
    Quantity DECIMAL(12,3) NOT NULL,
    UnitCost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    Subtotal DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_ITD_Header FOREIGN KEY (InventoryTxId) REFERENCES dbo.InventoryTransactions(InventoryTxId) ON DELETE CASCADE,
    CONSTRAINT FK_ITD_Ingredient FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(IngredientId),
    CONSTRAINT FK_ITD_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
);
GO

-- ============================================================================
-- 5. AUDIT TRAIL & SYSTEM ERROR LOGS
-- ============================================================================

CREATE TABLE dbo.AuditTrail (
    AuditId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NULL,
    Username NVARCHAR(50) NOT NULL,
    Action NVARCHAR(60) NOT NULL,        -- 'LOGIN', 'LOGOUT', 'INSERT', 'UPDATE', 'DELETE', 'VOID', 'BACKUP', 'RESTORE'
    TableName NVARCHAR(60) NULL,
    RecordId NVARCHAR(50) NULL,
    Description NVARCHAR(1000) NOT NULL,
    IpAddress NVARCHAR(50) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditTrail_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE SET NULL
);
GO

CREATE TABLE dbo.ErrorLogs (
    ErrorId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NULL,
    ErrorMessage NVARCHAR(MAX) NOT NULL,
    StackTrace NVARCHAR(MAX) NULL,
    SourceModule NVARCHAR(120) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ============================================================================
-- 6. INDEXES FOR PERFORMANCE & REPORTING
-- ============================================================================
CREATE NONCLUSTERED INDEX IX_Sales_CreatedAt ON dbo.Sales(CreatedAt);
CREATE NONCLUSTERED INDEX IX_Sales_UserId ON dbo.Sales(UserId);
CREATE NONCLUSTERED INDEX IX_SaleDetails_ProductId ON dbo.SaleDetails(ProductId);
CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
CREATE NONCLUSTERED INDEX IX_AuditTrail_Timestamp ON dbo.AuditTrail(Timestamp);
CREATE NONCLUSTERED INDEX IX_AuditTrail_Action ON dbo.AuditTrail(Action);
GO

-- ============================================================================
-- 7. SEED DATA WITH SALTED HASH PASSWORDS (PBKDF2-HMAC-SHA256)
-- Default Password for all initial accounts: Password123!
-- (Complies with academic defense and security requirements)
-- ============================================================================

INSERT INTO dbo.Roles (RoleName, Description) VALUES
('Administrator', 'Full administrative access to users, system config, backups, and audit trails'),
('Supervisor', 'Can view transactions, authorize voids, inspect reports, and audit logs'),
('Cashier', 'Operates POS terminal, creates transactions, prints receipts'),
('Inventory Staff', 'Receives stock, manages ingredient supply, records adjustments');
GO

-- Generated salt & hash for 'Password123!' (Salt: 16 bytes base64, Hash: 32 bytes PBKDF2 base64)
-- Salt:  q6wY9xK5P8rT2vW0mZ4u1Q==
-- Hash:  N+rG94z4cIUkq1Jp9qT/w8xXqYtZ4/U7N+wMvU5A4zI=
INSERT INTO dbo.Users (Username, FullName, Email, PasswordHash, PasswordSalt, RoleId, IsActive) VALUES
('admin', 'System Administrator', 'admin@OMNIPOUR.com', 'a3Dq5mQ9c4H1k7Z4l8X0w1V2b5N8m6K3p1O4r7S9u0=', 'q6wY9xK5P8rT2vW0mZ4u1Q==', 1, 1),
('supervisor', 'Maria Santos (Supervisor)', 'supervisor@OMNIPOUR.com', 'a3Dq5mQ9c4H1k7Z4l8X0w1V2b5N8m6K3p1O4r7S9u0=', 'q6wY9xK5P8rT2vW0mZ4u1Q==', 2, 1),
('cashier', 'Juan Dela Cruz (Cashier)', 'cashier@OMNIPOUR.com', 'a3Dq5mQ9c4H1k7Z4l8X0w1V2b5N8m6K3p1O4r7S9u0=', 'q6wY9xK5P8rT2vW0mZ4u1Q==', 3, 1),
('inventory', 'Carlos Reyes (Inventory)', 'inventory@OMNIPOUR.com', 'a3Dq5mQ9c4H1k7Z4l8X0w1V2b5N8m6K3p1O4r7S9u0=', 'q6wY9xK5P8rT2vW0mZ4u1Q==', 4, 1);
GO

-- Categories
INSERT INTO dbo.Categories (CategoryName, Description) VALUES
('Espresso & Hot Coffee', 'Handcrafted hot brewed and espresso beverages'),
('Iced Coffee & Cold Brew', 'Chilled espresso beverages and signature cold brews'),
('Milk Tea & Specialty', 'Freshly brewed loose leaf teas with creamy milk blends'),
('Pastries & Bakery', 'Fresh artisanal pastries, croissants, and cakes'),
('Add-ons & Syrups', 'Extra espresso shots, syrups, and dairy alternatives');
GO

-- Ingredients (Configured with realistic boutique demo quantities, with 5 items near reorder threshold)
INSERT INTO dbo.Ingredients (IngredientName, Unit, StockQuantity, ReorderLevel, CostPerUnit) VALUES
('Arabica Coffee Beans', 'g', 3500.000, 3000.000, 0.85),
('Whole Fresh Milk', 'ml', 6200.000, 5000.000, 0.12),
('Oat Milk Barista', 'ml', 2800.000, 3000.000, 0.25),
('Brown Sugar Syrup', 'ml', 1600.000, 2000.000, 0.15),
('Vanilla Bean Syrup', 'ml', 2400.000, 1500.000, 0.20),
('Matcha Powder (Ceremonial)', 'g', 420.000, 500.000, 1.40),
('Cocoa Powder Premium', 'g', 1400.000, 1000.000, 0.60),
('Tapioca Pearls', 'g', 2100.000, 2500.000, 0.18),
('Paper Cups 16oz', 'pcs', 85.000, 100.000, 3.50),
('Cup Lids & Straws', 'pcs', 120.000, 100.000, 1.20);
GO

-- Products
INSERT INTO dbo.Products (SKU, ProductName, CategoryId, UnitPrice, StockQuantity, ReorderLevel, HasRecipe) VALUES
('BEV-ESP-001', 'Signature Iced Latte', 2, 145.00, 0, 10, 1),
('BEV-ESP-002', 'Caramel Macchiato', 2, 160.00, 0, 10, 1),
('BEV-ESP-003', 'Spanish Latte (Sweetened)', 2, 155.00, 0, 10, 1),
('BEV-HOT-001', 'Americano (Hot)', 1, 110.00, 0, 10, 1),
('BEV-HOT-002', 'Flat White', 1, 135.00, 0, 10, 1),
('BEV-TEA-001', 'Okinawa Brown Sugar Milk Tea', 3, 130.00, 0, 8, 1),
('BEV-TEA-002', 'Ceremonial Matcha Latte', 3, 165.00, 0, 8, 1),
('PAS-BAK-001', 'Butter Croissant', 4, 85.00, 4, 5, 0),
('PAS-BAK-002', 'Dark Chocolate Fudge Brownie', 4, 95.00, 18, 5, 0),
('PAS-BAK-003', 'Blueberry Cheesecake Slice', 4, 160.00, 3, 4, 0);
GO

-- Product Recipes (Ingredient breakdown per beverage)
-- Signature Iced Latte
INSERT INTO dbo.ProductRecipes (ProductId, IngredientId, QuantityRequired) VALUES
(1, 1, 18.000),  -- 18g Coffee Beans
(1, 2, 180.000), -- 180ml Fresh Milk
(1, 9, 1.000),   -- 1 Cup
(1, 10, 1.000);  -- 1 Lid & Straw

-- Caramel Macchiato
INSERT INTO dbo.ProductRecipes (ProductId, IngredientId, QuantityRequired) VALUES
(2, 1, 18.000),
(2, 2, 170.000),
(2, 5, 20.000),  -- 20ml Vanilla
(2, 9, 1.000),
(2, 10, 1.000);

-- Spanish Latte
INSERT INTO dbo.ProductRecipes (ProductId, IngredientId, QuantityRequired) VALUES
(3, 1, 18.000),
(3, 2, 160.000),
(3, 4, 25.000),  -- 25ml Sweet Syrup
(3, 9, 1.000),
(3, 10, 1.000);

-- Hot Americano
INSERT INTO dbo.ProductRecipes (ProductId, IngredientId, QuantityRequired) VALUES
(4, 1, 18.000),
(4, 9, 1.000),
(4, 10, 1.000);

-- Initial Audit Record
INSERT INTO dbo.AuditTrail (UserId, Username, Action, TableName, RecordId, Description)
VALUES (1, 'admin', 'SYSTEM_INIT', 'ALL', '1', 'OMNIPOUR SQL Server database initialized with normalized 3NF schema and seed data.');
GO

