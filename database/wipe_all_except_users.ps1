$connStr = "Server=(localdb)\MSSQLLocalDB;Database=OmniPourDB;Integrated Security=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$sql = @"
DELETE FROM dbo.ErrorLogs;
DELETE FROM dbo.AuditTrail;
DELETE FROM dbo.InventoryTransactionDetails;
DELETE FROM dbo.InventoryTransactions;
DELETE FROM dbo.SaleDetails;
DELETE FROM dbo.Sales;
DELETE FROM dbo.ProductRecipes;
DELETE FROM dbo.Products;
DELETE FROM dbo.Ingredients;
DELETE FROM dbo.Categories;

DBCC CHECKIDENT ('dbo.Categories', RESEED, 0);
DBCC CHECKIDENT ('dbo.Ingredients', RESEED, 0);
DBCC CHECKIDENT ('dbo.Products', RESEED, 0);
DBCC CHECKIDENT ('dbo.Sales', RESEED, 0);
DBCC CHECKIDENT ('dbo.SaleDetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.InventoryTransactions', RESEED, 0);
DBCC CHECKIDENT ('dbo.InventoryTransactionDetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.AuditTrail', RESEED, 0);
DBCC CHECKIDENT ('dbo.ErrorLogs', RESEED, 0);
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$cmd.ExecuteNonQuery() | Out-Null
$conn.Close()

Write-Output "DATABASE PURGED: All catalog, recipe, and transaction data deleted. Users and Roles preserved."
