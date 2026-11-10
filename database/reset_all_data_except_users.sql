USE [OmniPourDB];
GO

-- ============================================================================
-- OMNIPOUR DATABASE PURGE SCRIPT
-- Clears all transaction, inventory, recipe, product, and category records
-- PRESERVES: dbo.Roles and dbo.Users (System accounts remain active)
-- ============================================================================

-- 1. Delete transactional history
DELETE FROM dbo.ErrorLogs;
DELETE FROM dbo.AuditTrail;
DELETE FROM dbo.InventoryTransactionDetails;
DELETE FROM dbo.InventoryTransactions;
DELETE FROM dbo.SaleDetails;
DELETE FROM dbo.Sales;

-- 2. Delete menu and inventory master records
DELETE FROM dbo.ProductRecipes;
DELETE FROM dbo.Products;
DELETE FROM dbo.Ingredients;
DELETE FROM dbo.Categories;

-- 3. Reseed identity counters to 0
DBCC CHECKIDENT ('dbo.Categories', RESEED, 0);
DBCC CHECKIDENT ('dbo.Ingredients', RESEED, 0);
DBCC CHECKIDENT ('dbo.Products', RESEED, 0);
DBCC CHECKIDENT ('dbo.Sales', RESEED, 0);
DBCC CHECKIDENT ('dbo.SaleDetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.InventoryTransactions', RESEED, 0);
DBCC CHECKIDENT ('dbo.InventoryTransactionDetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.AuditTrail', RESEED, 0);
DBCC CHECKIDENT ('dbo.ErrorLogs', RESEED, 0);

PRINT 'All catalog, recipe, and transaction data successfully wiped. Users and Roles preserved.';
GO
