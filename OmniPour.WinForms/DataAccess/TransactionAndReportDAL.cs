using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.Utilities;
using Microsoft.Data.SqlClient;

namespace OmniPour.WinForms.DataAccess
{
    public static class SaleDAL
    {
        public static (bool success, string message, int saleId, string invoiceNumber) ProcessSale(Sale sale)
        {
            if (sale.Items == null || sale.Items.Count == 0)
                return (false, "Cannot process a sale with no items.", 0, "");

            if (sale.Subtotal < 0 || sale.TaxAmount < 0 || sale.DiscountAmount < 0 ||
                sale.GrandTotal < 0 || sale.AmountTendered < 0 || sale.ChangeDue < 0 ||
                sale.Items.Any(item => item.Quantity <= 0 || item.UnitPrice < 0 || item.Subtotal < 0))
                return (false, "Sale amounts, prices, and quantities cannot be negative or zero where a quantity is required.", 0, "");

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.RepeatableRead);

            try
            {
                // 1. Generate Unique Invoice Number (INV-YYYYMMDD-XXXXX)
                string datePrefix = DateTime.Now.ToString("yyyyMMdd");
                string countSql = "SELECT COUNT(*) FROM dbo.Sales WITH (TABLOCKX) WHERE InvoiceNumber LIKE 'INV-' + @DatePrefix + '-%'";
                using var countCmd = new SqlCommand(countSql, conn, trans);
                countCmd.Parameters.AddWithValue("@DatePrefix", datePrefix);
                int todayCount = Convert.ToInt32(countCmd.ExecuteScalar()) + 1;
                string invoiceNumber = $"INV-{datePrefix}-{todayCount:D4}";

                // 2. Validate and Deduct Stock / Check Recipe Ingredients
                foreach (var item in sale.Items)
                {
                    // Check Product
                    using var prodCheckCmd = new SqlCommand(
                        "SELECT ProductId, ProductName, StockQuantity, HasRecipe FROM dbo.Products WITH (UPDLOCK) WHERE ProductId = @Pid", conn, trans);
                    prodCheckCmd.Parameters.AddWithValue("@Pid", item.ProductId);
                    using var reader = prodCheckCmd.ExecuteReader();

                    if (!reader.Read())
                    {
                        reader.Close();
                        trans.Rollback();
                        return (false, $"Product ID {item.ProductId} not found.", 0, "");
                    }

                    int currentProdStock = Convert.ToInt32(reader["StockQuantity"]);
                    bool hasRecipe = Convert.ToBoolean(reader["HasRecipe"]);
                    string productName = reader["ProductName"].ToString() ?? "";
                    reader.Close();

                    // If it does not have a recipe or is a direct stock item, check stock
                    if (!hasRecipe && currentProdStock < item.Quantity)
                    {
                        trans.Rollback();
                        return (false, $"Insufficient stock for '{productName}'. Available: {currentProdStock}, Requested: {item.Quantity}.", 0, "");
                    }

                    // Deduct product stock (if available or tracked)
                    using var deductProdCmd = new SqlCommand(
                        "UPDATE dbo.Products SET StockQuantity = CASE WHEN StockQuantity >= @Qty THEN StockQuantity - @Qty ELSE 0 END WHERE ProductId = @Pid", conn, trans);
                    deductProdCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    deductProdCmd.Parameters.AddWithValue("@Pid", item.ProductId);
                    deductProdCmd.ExecuteNonQuery();

                    // If has recipe, deduct ingredient raw materials atomically
                    if (hasRecipe)
                    {
                        using var recipeCmd = new SqlCommand(
                            @"SELECT pr.IngredientId, i.IngredientName, i.StockQuantity, pr.QuantityRequired
                              FROM dbo.ProductRecipes pr
                              INNER JOIN dbo.Ingredients i WITH (UPDLOCK) ON pr.IngredientId = i.IngredientId
                              WHERE pr.ProductId = @Pid", conn, trans);
                        recipeCmd.Parameters.AddWithValue("@Pid", item.ProductId);

                        var ingList = new List<(int id, string name, decimal stock, decimal requiredPerUnit)>();
                        using (var ingReader = recipeCmd.ExecuteReader())
                        {
                            while (ingReader.Read())
                            {
                                ingList.Add((
                                    Convert.ToInt32(ingReader["IngredientId"]),
                                    ingReader["IngredientName"].ToString() ?? "",
                                    Convert.ToDecimal(ingReader["StockQuantity"]),
                                    Convert.ToDecimal(ingReader["QuantityRequired"])
                                ));
                            }
                        }

                        foreach (var ing in ingList)
                        {
                            decimal totalNeeded = ing.requiredPerUnit * item.Quantity;
                            if (ing.stock < totalNeeded)
                            {
                                trans.Rollback();
                                return (false, $"Insufficient ingredient '{ing.name}' for '{productName}'. In stock: {ing.stock:N2}, Needed: {totalNeeded:N2}.", 0, "");
                            }

                            using var deductIngCmd = new SqlCommand(
                                "UPDATE dbo.Ingredients SET StockQuantity = StockQuantity - @Needed WHERE IngredientId = @Iid", conn, trans);
                            deductIngCmd.Parameters.AddWithValue("@Needed", totalNeeded);
                            deductIngCmd.Parameters.AddWithValue("@Iid", ing.id);
                            deductIngCmd.ExecuteNonQuery();
                        }
                    }
                }

                // 3. Insert Sale Header
                const string insertSaleSql = @"
                    INSERT INTO dbo.Sales (
                        InvoiceNumber, UserId, CustomerName, Subtotal, TaxAmount, DiscountAmount,
                        GrandTotal, AmountTendered, ChangeDue, PaymentMethod, PaymentStatus
                    )
                    OUTPUT INSERTED.SaleId
                    VALUES (
                        @Inv, @Uid, @Cust, @Subtotal, @Tax, @Disc,
                        @Grand, @Tender, @Change, @Method, 'Paid'
                    )";

                using var saleCmd = new SqlCommand(insertSaleSql, conn, trans);
                saleCmd.Parameters.AddWithValue("@Inv", invoiceNumber);
                saleCmd.Parameters.AddWithValue("@Uid", sale.UserId);
                saleCmd.Parameters.AddWithValue("@Cust", string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName.Trim());
                saleCmd.Parameters.AddWithValue("@Subtotal", sale.Subtotal);
                saleCmd.Parameters.AddWithValue("@Tax", sale.TaxAmount);
                saleCmd.Parameters.AddWithValue("@Disc", sale.DiscountAmount);
                saleCmd.Parameters.AddWithValue("@Grand", sale.GrandTotal);
                saleCmd.Parameters.AddWithValue("@Tender", sale.AmountTendered);
                saleCmd.Parameters.AddWithValue("@Change", sale.ChangeDue);
                saleCmd.Parameters.AddWithValue("@Method", sale.PaymentMethod);

                int saleId = Convert.ToInt32(saleCmd.ExecuteScalar());

                // 4. Insert Sale Details
                const string insertDetailSql = @"
                    INSERT INTO dbo.SaleDetails (SaleId, ProductId, ProductName, UnitPrice, Quantity, Subtotal)
                    VALUES (@SaleId, @Pid, @Pname, @Price, @Qty, @Sub)";

                foreach (var item in sale.Items)
                {
                    using var detailCmd = new SqlCommand(insertDetailSql, conn, trans);
                    detailCmd.Parameters.AddWithValue("@SaleId", saleId);
                    detailCmd.Parameters.AddWithValue("@Pid", item.ProductId);
                    detailCmd.Parameters.AddWithValue("@Pname", item.ProductName);
                    detailCmd.Parameters.AddWithValue("@Price", item.UnitPrice);
                    detailCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    detailCmd.Parameters.AddWithValue("@Sub", item.Subtotal);
                    detailCmd.ExecuteNonQuery();
                }

                // 5. Commit Transaction
                trans.Commit();

                // Audit log
                AuditDAL.Log(sale.UserId, UserSession.CurrentUser?.Username ?? "CASHIER", "SALE_COMPLETE", "Sales", saleId.ToString(),
                    $"Completed sale #{invoiceNumber} for total ₱{sale.GrandTotal:N2} ({sale.Items.Count} items).");

                return (true, "Sale processed successfully.", saleId, invoiceNumber);
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                Logger.LogException(ex, "SaleDAL.ProcessSale", DatabaseHelper.GetConnectionString());
                return (false, "Transaction failed and was safely rolled back. Details: " + ex.Message, 0, "");
            }
        }

        public static (bool success, string message) VoidSale(int saleId, int voidedByUserId, string reason)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Verify sale status
                using var checkCmd = new SqlCommand("SELECT InvoiceNumber, PaymentStatus FROM dbo.Sales WITH (UPDLOCK) WHERE SaleId = @Id", conn, trans);
                checkCmd.Parameters.AddWithValue("@Id", saleId);
                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                {
                    reader.Close();
                    trans.Rollback();
                    return (false, "Sale not found.");
                }

                string status = reader["PaymentStatus"].ToString() ?? "";
                string invNum = reader["InvoiceNumber"].ToString() ?? "";
                reader.Close();

                if (status == "Voided")
                {
                    trans.Rollback();
                    return (false, "This transaction has already been voided.");
                }

                // Retrieve details for stock restoration
                var details = new List<(int prodId, int qty)>();
                using var detCmd = new SqlCommand("SELECT ProductId, Quantity FROM dbo.SaleDetails WHERE SaleId = @Id", conn, trans);
                detCmd.Parameters.AddWithValue("@Id", saleId);
                using (var r2 = detCmd.ExecuteReader())
                {
                    while (r2.Read())
                    {
                        details.Add((Convert.ToInt32(r2["ProductId"]), Convert.ToInt32(r2["Quantity"])));
                    }
                }

                // Restore products & recipes
                foreach (var d in details)
                {
                    using var restProdCmd = new SqlCommand("UPDATE dbo.Products SET StockQuantity = StockQuantity + @Qty WHERE ProductId = @Pid", conn, trans);
                    restProdCmd.Parameters.AddWithValue("@Qty", d.qty);
                    restProdCmd.Parameters.AddWithValue("@Pid", d.prodId);
                    restProdCmd.ExecuteNonQuery();

                    // Restore ingredients
                    using var restIngCmd = new SqlCommand(@"
                        UPDATE i
                        SET i.StockQuantity = i.StockQuantity + (pr.QuantityRequired * @Qty)
                        FROM dbo.Ingredients i
                        INNER JOIN dbo.ProductRecipes pr ON i.IngredientId = pr.IngredientId
                        WHERE pr.ProductId = @Pid", conn, trans);
                    restIngCmd.Parameters.AddWithValue("@Qty", d.qty);
                    restIngCmd.Parameters.AddWithValue("@Pid", d.prodId);
                    restIngCmd.ExecuteNonQuery();
                }

                // Mark Sale as Voided
                using var voidCmd = new SqlCommand(@"
                    UPDATE dbo.Sales
                    SET PaymentStatus = 'Voided',
                        VoidedAt = SYSUTCDATETIME(),
                        VoidReason = @Reason,
                        VoidedByUserId = @VoidedBy
                    WHERE SaleId = @Id", conn, trans);
                voidCmd.Parameters.AddWithValue("@Reason", reason.Trim());
                voidCmd.Parameters.AddWithValue("@VoidedBy", voidedByUserId);
                voidCmd.Parameters.AddWithValue("@Id", saleId);
                voidCmd.ExecuteNonQuery();

                trans.Commit();

                AuditDAL.Log(voidedByUserId, UserSession.CurrentUser?.Username ?? "USER", "SALE_VOID", "Sales", saleId.ToString(),
                    $"Voided transaction #{invNum}. Reason: {reason}");

                return (true, $"Transaction #{invNum} was successfully voided and inventory restored.");
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                Logger.LogException(ex, "SaleDAL.VoidSale", DatabaseHelper.GetConnectionString());
                return (false, "Failed to void transaction: " + ex.Message);
            }
        }

        public static List<Sale> GetSales(DateTime? fromDate = null, DateTime? toDate = null, string? statusFilter = null, string? searchKeyword = null)
        {
            var list = new List<Sale>();
            string sql = @"
                SELECT s.SaleId, s.InvoiceNumber, s.UserId, u.FullName AS CashierName,
                       s.CustomerName, s.Subtotal, s.TaxAmount, s.DiscountAmount,
                       s.GrandTotal, s.AmountTendered, s.ChangeDue, s.PaymentMethod,
                       s.PaymentStatus, s.CreatedAt, s.VoidedAt, s.VoidReason,
                       s.VoidedByUserId, vu.FullName AS VoidedByName, s.RowVersion
                FROM dbo.Sales s
                INNER JOIN dbo.Users u ON s.UserId = u.UserId
                LEFT JOIN dbo.Users vu ON s.VoidedByUserId = vu.UserId
                WHERE (1 = 1) ";

            var parameters = new List<SqlParameter>();

            if (fromDate.HasValue)
            {
                sql += " AND s.CreatedAt >= @FromDate ";
                parameters.Add(new SqlParameter("@FromDate", fromDate.Value.Date));
            }
            if (toDate.HasValue)
            {
                sql += " AND s.CreatedAt < @ToDate ";
                parameters.Add(new SqlParameter("@ToDate", toDate.Value.Date.AddDays(1)));
            }
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                sql += " AND s.PaymentStatus = @Status ";
                parameters.Add(new SqlParameter("@Status", statusFilter));
            }
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                sql += " AND (s.InvoiceNumber LIKE @Kw OR s.CustomerName LIKE @Kw) ";
                parameters.Add(new SqlParameter("@Kw", $"%{searchKeyword.Trim()}%"));
            }

            sql += " ORDER BY s.CreatedAt DESC";

            var dt = DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Sale
                {
                    SaleId = Convert.ToInt32(row["SaleId"]),
                    InvoiceNumber = row["InvoiceNumber"].ToString() ?? "",
                    UserId = Convert.ToInt32(row["UserId"]),
                    CashierName = row["CashierName"].ToString() ?? "",
                    CustomerName = row["CustomerName"].ToString() ?? "",
                    Subtotal = Convert.ToDecimal(row["Subtotal"]),
                    TaxAmount = Convert.ToDecimal(row["TaxAmount"]),
                    DiscountAmount = Convert.ToDecimal(row["DiscountAmount"]),
                    GrandTotal = Convert.ToDecimal(row["GrandTotal"]),
                    AmountTendered = Convert.ToDecimal(row["AmountTendered"]),
                    ChangeDue = Convert.ToDecimal(row["ChangeDue"]),
                    PaymentMethod = row["PaymentMethod"].ToString() ?? "",
                    PaymentStatus = row["PaymentStatus"].ToString() ?? "",
                    CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                    VoidedAt = row["VoidedAt"] != DBNull.Value ? Convert.ToDateTime(row["VoidedAt"]) : null,
                    VoidReason = row["VoidReason"]?.ToString() ?? "",
                    VoidedByUserId = row["VoidedByUserId"] != DBNull.Value ? Convert.ToInt32(row["VoidedByUserId"]) : null,
                    VoidedByName = row["VoidedByName"]?.ToString() ?? "",
                    RowVersion = (byte[])row["RowVersion"]
                });
            }
            return list;
        }

        public static List<SaleDetail> GetSaleDetails(int saleId)
        {
            var list = new List<SaleDetail>();
            const string sql = "SELECT SaleDetailId, SaleId, ProductId, ProductName, UnitPrice, Quantity, Subtotal FROM dbo.SaleDetails WHERE SaleId = @Id";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Id", saleId) });
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new SaleDetail
                {
                    SaleDetailId = Convert.ToInt32(row["SaleDetailId"]),
                    SaleId = Convert.ToInt32(row["SaleId"]),
                    ProductId = Convert.ToInt32(row["ProductId"]),
                    ProductName = row["ProductName"].ToString() ?? "",
                    UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
                    Quantity = Convert.ToInt32(row["Quantity"]),
                    Subtotal = Convert.ToDecimal(row["Subtotal"])
                });
            }
            return list;
        }

        public static DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();

            // 1. Today's sales & count
            const string todaySql = @"
                SELECT ISNULL(SUM(GrandTotal), 0) AS TodaySales, COUNT(*) AS TodayCount
                FROM dbo.Sales
                WHERE PaymentStatus = 'Paid'
                  AND CAST(CreatedAt AS DATE) = CAST(SYSUTCDATETIME() AS DATE)";
            var dtToday = DatabaseHelper.ExecuteQuery(todaySql);
            if (dtToday.Rows.Count > 0)
            {
                stats.TodaySales = Convert.ToDecimal(dtToday.Rows[0]["TodaySales"]);
                stats.TodayTransactionCount = Convert.ToInt32(dtToday.Rows[0]["TodayCount"]);
            }

            // 2. Monthly sales
            const string monthSql = @"
                SELECT ISNULL(SUM(GrandTotal), 0) AS MonthSales
                FROM dbo.Sales
                WHERE PaymentStatus = 'Paid'
                  AND MONTH(CreatedAt) = MONTH(SYSUTCDATETIME())
                  AND YEAR(CreatedAt) = YEAR(SYSUTCDATETIME())";
            stats.MonthlySales = Convert.ToDecimal(DatabaseHelper.ExecuteScalar(monthSql) ?? 0);

            // 3. Low stock count for products
            const string lowProdSql = "SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1 AND StockQuantity <= ReorderLevel";
            stats.LowStockProductCount = Convert.ToInt32(DatabaseHelper.ExecuteScalar(lowProdSql) ?? 0);

            // 4. Low stock count for ingredients
            const string lowIngSql = "SELECT COUNT(*) FROM dbo.Ingredients WHERE IsActive = 1 AND StockQuantity <= ReorderLevel";
            stats.LowStockIngredientCount = Convert.ToInt32(DatabaseHelper.ExecuteScalar(lowIngSql) ?? 0);

            // 5. Active users count
            const string userSql = "SELECT COUNT(*) FROM dbo.Users WHERE IsActive = 1";
            stats.ActiveUsersCount = Convert.ToInt32(DatabaseHelper.ExecuteScalar(userSql) ?? 0);

            return stats;
        }

        public static DataTable GetDailySalesReport(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
                SELECT CAST(CreatedAt AS DATE) AS [Date],
                       COUNT(*) AS [TransactionsCount],
                       SUM(Subtotal) AS [SubtotalSales],
                       SUM(TaxAmount) AS [TotalTax],
                       SUM(DiscountAmount) AS [TotalDiscounts],
                       SUM(GrandTotal) AS [NetSales]
                FROM dbo.Sales
                WHERE PaymentStatus = 'Paid'
                  AND CreatedAt >= @FromDate AND CreatedAt < @ToDate
                GROUP BY CAST(CreatedAt AS DATE)
                ORDER BY [Date] DESC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@FromDate", fromDate.Date),
                new SqlParameter("@ToDate", toDate.Date.AddDays(1))
            });
        }

        public static DataTable GetSalesByProductReport(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
                SELECT c.CategoryName AS [Category],
                       sd.ProductName AS [Product],
                       SUM(sd.Quantity) AS [QuantitySold],
                       AVG(sd.UnitPrice) AS [AvgPrice],
                       SUM(sd.Subtotal) AS [TotalRevenue]
                FROM dbo.SaleDetails sd
                INNER JOIN dbo.Sales s ON sd.SaleId = s.SaleId
                INNER JOIN dbo.Products p ON sd.ProductId = p.ProductId
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                WHERE s.PaymentStatus = 'Paid'
                  AND s.CreatedAt >= @FromDate AND s.CreatedAt < @ToDate
                GROUP BY c.CategoryName, sd.ProductName
                ORDER BY [TotalRevenue] DESC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@FromDate", fromDate.Date),
                new SqlParameter("@ToDate", toDate.Date.AddDays(1))
            });
        }

        public static DataTable GetLowStockReport()
        {
            const string sql = @"
                SELECT 'Ready Product' AS [ItemType],
                       p.ProductName AS [ItemName],
                       'pcs' AS [Unit],
                       CAST(p.StockQuantity AS DECIMAL(12,2)) AS [CurrentStock],
                       CAST(p.ReorderLevel AS DECIMAL(12,2)) AS [ReorderLevel],
                       CASE WHEN p.StockQuantity = 0 THEN 'OUT OF STOCK' ELSE 'LOW STOCK' END AS [Status]
                FROM dbo.Products p
                WHERE p.IsActive = 1 AND p.StockQuantity <= p.ReorderLevel
                UNION ALL
                SELECT 'Raw Ingredient' AS [ItemType],
                       i.IngredientName AS [ItemName],
                       i.Unit AS [Unit],
                       i.StockQuantity AS [CurrentStock],
                       i.ReorderLevel AS [ReorderLevel],
                       CASE WHEN i.StockQuantity = 0 THEN 'OUT OF STOCK' ELSE 'LOW STOCK' END AS [Status]
                FROM dbo.Ingredients i
                WHERE i.IsActive = 1 AND i.StockQuantity <= i.ReorderLevel
                ORDER BY [Status] DESC, [CurrentStock] ASC";

            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable GetInventoryStatusReport()
        {
            const string sql = @"
                SELECT [ItemType], [ItemName], [Unit], [CurrentStock], [ReorderLevel], [Status]
                FROM (
                    SELECT 'Ready Product' AS [ItemType],
                           p.ProductName AS [ItemName],
                           'pcs' AS [Unit],
                           CAST(p.StockQuantity AS DECIMAL(18,3)) AS [CurrentStock],
                           CAST(p.ReorderLevel AS DECIMAL(18,3)) AS [ReorderLevel],
                           CASE WHEN p.StockQuantity = 0 THEN 'OUT OF STOCK'
                                WHEN p.StockQuantity <= p.ReorderLevel THEN 'LOW STOCK'
                                ELSE 'IN STOCK' END AS [Status]
                    FROM dbo.Products p
                    WHERE p.IsActive = 1
                    UNION ALL
                    SELECT 'Raw Ingredient' AS [ItemType],
                           i.IngredientName AS [ItemName],
                           i.Unit AS [Unit],
                           CAST(i.StockQuantity AS DECIMAL(18,3)) AS [CurrentStock],
                           CAST(i.ReorderLevel AS DECIMAL(18,3)) AS [ReorderLevel],
                           CASE WHEN i.StockQuantity = 0 THEN 'OUT OF STOCK'
                                WHEN i.StockQuantity <= i.ReorderLevel THEN 'LOW STOCK'
                                ELSE 'IN STOCK' END AS [Status]
                    FROM dbo.Ingredients i
                    WHERE i.IsActive = 1
                ) AS InventoryStatus
                ORDER BY CASE [Status] WHEN 'OUT OF STOCK' THEN 0 WHEN 'LOW STOCK' THEN 1 ELSE 2 END,
                         [ItemType], [ItemName]";

            return DatabaseHelper.ExecuteQuery(sql);
        }
    }

    public static class InventoryTransactionDAL
    {
        public static (bool success, string message) ProcessTransaction(InventoryTransaction tx)
        {
            if (tx.Details == null || tx.Details.Count == 0)
                return (false, "Please add at least one item to this inventory transaction.");

            if (tx.TotalCost < 0 || tx.Details.Any(detail => detail.Quantity <= 0 || detail.UnitCost < 0 || detail.Subtotal < 0))
                return (false, "Inventory quantities must be greater than zero, and costs cannot be negative.");

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Reference number: TX-YYYYMMDD-XXXX
                string prefix = tx.TransactionType == "Delivery" ? "RCV" : "ADJ";
                string dateStr = DateTime.Now.ToString("yyyyMMdd");
                string countSql = "SELECT COUNT(*) FROM dbo.InventoryTransactions WITH (TABLOCKX) WHERE ReferenceNumber LIKE @Pref + '-%'";
                using var cntCmd = new SqlCommand(countSql, conn, trans);
                cntCmd.Parameters.AddWithValue("@Pref", $"{prefix}-{dateStr}");
                int count = Convert.ToInt32(cntCmd.ExecuteScalar()) + 1;
                string refNum = $"{prefix}-{dateStr}-{count:D4}";

                const string insertHeaderSql = @"
                    INSERT INTO dbo.InventoryTransactions (ReferenceNumber, UserId, TransactionType, SupplierName, TotalCost, Notes)
                    OUTPUT INSERTED.InventoryTxId
                    VALUES (@Ref, @Uid, @Type, @Sup, @Cost, @Notes)";

                using var headCmd = new SqlCommand(insertHeaderSql, conn, trans);
                headCmd.Parameters.AddWithValue("@Ref", refNum);
                headCmd.Parameters.AddWithValue("@Uid", tx.UserId);
                headCmd.Parameters.AddWithValue("@Type", tx.TransactionType);
                headCmd.Parameters.AddWithValue("@Sup", (object?)tx.SupplierName ?? DBNull.Value);
                headCmd.Parameters.AddWithValue("@Cost", tx.TotalCost);
                headCmd.Parameters.AddWithValue("@Notes", (object?)tx.Notes ?? DBNull.Value);

                int txId = Convert.ToInt32(headCmd.ExecuteScalar());

                const string insertDetailSql = @"
                    INSERT INTO dbo.InventoryTransactionDetails (InventoryTxId, IngredientId, ProductId, ItemType, ItemName, Quantity, UnitCost, Subtotal)
                    VALUES (@TxId, @Iid, @Pid, @Type, @Name, @Qty, @Cost, @Sub)";

                foreach (var d in tx.Details)
                {
                    using var dCmd = new SqlCommand(insertDetailSql, conn, trans);
                    dCmd.Parameters.AddWithValue("@TxId", txId);
                    dCmd.Parameters.AddWithValue("@Iid", (object?)d.IngredientId ?? DBNull.Value);
                    dCmd.Parameters.AddWithValue("@Pid", (object?)d.ProductId ?? DBNull.Value);
                    dCmd.Parameters.AddWithValue("@Type", d.ItemType);
                    dCmd.Parameters.AddWithValue("@Name", d.ItemName);
                    dCmd.Parameters.AddWithValue("@Qty", d.Quantity);
                    dCmd.Parameters.AddWithValue("@Cost", d.UnitCost);
                    dCmd.Parameters.AddWithValue("@Sub", d.Subtotal);
                    dCmd.ExecuteNonQuery();

                    // Apply inventory adjustment (+ for delivery, - for spoilage/negative adjustments)
                    decimal multiplier = tx.TransactionType == "Spoilage" ? -1 : 1;

                    if (d.ItemType == "Ingredient" && d.IngredientId.HasValue)
                    {
                        using var updCmd = new SqlCommand(@"
                            UPDATE dbo.Ingredients
                            SET StockQuantity = CASE 
                                WHEN (StockQuantity + (@Qty * @Mul)) >= 0 THEN StockQuantity + (@Qty * @Mul)
                                ELSE 0 END
                            WHERE IngredientId = @Iid", conn, trans);
                        updCmd.Parameters.AddWithValue("@Qty", d.Quantity);
                        updCmd.Parameters.AddWithValue("@Mul", multiplier);
                        updCmd.Parameters.AddWithValue("@Iid", d.IngredientId.Value);
                        updCmd.ExecuteNonQuery();
                    }
                    else if (d.ItemType == "Product" && d.ProductId.HasValue)
                    {
                        using var updCmd = new SqlCommand(@"
                            UPDATE dbo.Products
                            SET StockQuantity = CASE 
                                WHEN (StockQuantity + CAST((@Qty * @Mul) AS INT)) >= 0 THEN StockQuantity + CAST((@Qty * @Mul) AS INT)
                                ELSE 0 END
                            WHERE ProductId = @Pid", conn, trans);
                        updCmd.Parameters.AddWithValue("@Qty", d.Quantity);
                        updCmd.Parameters.AddWithValue("@Mul", multiplier);
                        updCmd.Parameters.AddWithValue("@Pid", d.ProductId.Value);
                        updCmd.ExecuteNonQuery();
                    }
                }

                trans.Commit();

                AuditDAL.Log(tx.UserId, UserSession.CurrentUser?.Username ?? "INVENTORY", "INVENTORY_TX", "InventoryTransactions", txId.ToString(),
                    $"{tx.TransactionType} recorded #{refNum} with {tx.Details.Count} items.");

                return (true, $"{tx.TransactionType} transaction #{refNum} recorded successfully.");
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                Logger.LogException(ex, "InventoryTransactionDAL.ProcessTransaction", DatabaseHelper.GetConnectionString());
                return (false, "Transaction failed and rolled back: " + ex.Message);
            }
        }
    }

    public static class DatabaseMaintenanceDAL
    {
        public static (bool success, string message) BackupDatabase(string destinationFilePath)
        {
            try
            {
                string sql = @"
                    BACKUP DATABASE [OmniPourDB]
                    TO DISK = @DestPath
                    WITH FORMAT, MEDIANAME = 'OMNIPOURBackup', NAME = 'Full Backup of OmniPourDB';";

                DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@DestPath", destinationFilePath) });

                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "ADMIN", "BACKUP", "Database", "OmniPourDB",
                    $"Database successfully backed up to {destinationFilePath}");

                return (true, "Database backup completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "DatabaseMaintenanceDAL.BackupDatabase");
                return (false, "Backup failed: " + ex.Message);
            }
        }

        public static (bool success, string message) RestoreDatabase(string sourceFilePath)
        {
            try
            {
                // Connect to master to restore OmniPourDB
                var builder = new SqlConnectionStringBuilder(DatabaseHelper.GetConnectionString())
                {
                    InitialCatalog = "master"
                };

                using var conn = new SqlConnection(builder.ConnectionString);
                conn.Open();

                string sql = @"
                    ALTER DATABASE [OmniPourDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    RESTORE DATABASE [OmniPourDB]
                    FROM DISK = @SourcePath
                    WITH REPLACE;
                    ALTER DATABASE [OmniPourDB] SET MULTI_USER;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 120; // 2 minutes
                cmd.Parameters.AddWithValue("@SourcePath", sourceFilePath);
                cmd.ExecuteNonQuery();

                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "ADMIN", "RESTORE", "Database", "OmniPourDB",
                    $"Database restored from {sourceFilePath}");

                return (true, "Database successfully restored from backup.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "DatabaseMaintenanceDAL.RestoreDatabase");
                return (false, "Restore failed: " + ex.Message);
            }
        }
    }
}
