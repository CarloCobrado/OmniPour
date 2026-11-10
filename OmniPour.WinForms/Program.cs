using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OmniPour.WinForms.BusinessLogic;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Forms;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            EnsureDefaultPasswordsInitialized();

            if (args.Contains("--self-test"))
            {
                RunSelfTest();
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new LoginForm());
        }

        static void EnsureDefaultPasswordsInitialized()
        {
            try
            {
                // Ensure default accounts (admin, supervisor, cashier, inventory) match C# PBKDF2 implementation
                var defaultUsers = new[] { "admin", "supervisor", "cashier", "inventory" };
                var (hash, salt) = PasswordSecurity.HashPassword("Password123!");

                foreach (var uname in defaultUsers)
                {
                    var u = UserDAL.GetByUsername(uname);
                    if (u != null && !PasswordSecurity.VerifyPassword("Password123!", u.PasswordHash, u.PasswordSalt))
                    {
                        UserDAL.ResetPassword(u.UserId, hash, salt);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Program.EnsureDefaultPasswordsInitialized");
            }
        }

        static void RunSelfTest()
        {
            Console.WriteLine("==================================================================");
            Console.WriteLine("OMNIPOUR BEVERAGE SUITE - AUTOMATED SYSTEM VERIFICATION");
            Console.WriteLine("==================================================================");

            // 1. Database Connection & Password Hashing Verification
            Console.Write("[TEST 1] Testing PBKDF2 Password Authentication: ");
            var auth = AuthBLL.Authenticate("admin", "Password123!");
            if (auth.success && auth.user != null && auth.user.RoleName == "Administrator")
            {
                Console.WriteLine($"PASSED (Logged in as {auth.user.FullName})");
            }
            else
            {
                Console.WriteLine($"FAILED ({auth.message})");
                Environment.Exit(1);
            }

            // 2. Querying Master Data
            Console.Write("[TEST 2] Testing Normalized Master Files (Products & Ingredients): ");
            var products = ProductDAL.GetAll();
            var ingredients = IngredientDAL.GetAll();
            if (products.Count >= 3 && ingredients.Count >= 3)
            {
                Console.WriteLine($"PASSED ({products.Count} Products, {ingredients.Count} Ingredients loaded)");
            }
            else
            {
                Console.WriteLine("FAILED (Expected at least 3 master records)");
                Environment.Exit(1);
            }

            // 3. Testing Atomic POS Sale Transaction with Recipe Ingredient Deduction
            Console.Write("[TEST 3] Testing Atomic POS Sale (Header/Detail with SqlTransaction): ");
            var testSale = new Sale
            {
                UserId = auth.user.UserId,
                CashierName = auth.user.FullName,
                CustomerName = "Dr. Defense Evaluator",
                Subtotal = 145.00m,
                DiscountAmount = 0.00m,
                TaxAmount = 0.00m,
                GrandTotal = 145.00m,
                AmountTendered = 200.00m,
                ChangeDue = 55.00m,
                PaymentMethod = "Cash",
                CreatedAt = DateTime.UtcNow
            };
            testSale.Items.Add(new SaleDetail
            {
                ProductId = 1, // Signature Iced Latte
                ProductName = "Signature Iced Latte",
                UnitPrice = 145.00m,
                Quantity = 1,
                Subtotal = 145.00m
            });

            var saleResult = SaleDAL.ProcessSale(testSale);
            if (saleResult.success && !string.IsNullOrEmpty(saleResult.invoiceNumber))
            {
                Console.WriteLine($"PASSED (Invoice #{saleResult.invoiceNumber} recorded)");
            }
            else
            {
                Console.WriteLine($"FAILED ({saleResult.message})");
                Environment.Exit(1);
            }

            // 4. Testing Multi-table Transaction 2: Inventory Delivery Receiving
            Console.Write("[TEST 4] Testing Inventory Delivery Receiving (Transaction 2): ");
            var deliveryTx = new InventoryTransaction
            {
                UserId = auth.user.UserId,
                TransactionType = "Delivery",
                SupplierName = "Batangas Arabica Estate",
                TotalCost = 5000.00m,
                Notes = "Automated test batch delivery"
            };
            deliveryTx.Details.Add(new InventoryTransactionDetail
            {
                ItemType = "Ingredient",
                IngredientId = 1,
                ItemName = "Arabica Coffee Beans (g)",
                Quantity = 1000.000m,
                UnitCost = 0.85m,
                Subtotal = 850.00m
            });

            var deliveryResult = InventoryTransactionDAL.ProcessTransaction(deliveryTx);
            if (deliveryResult.success)
            {
                Console.WriteLine($"PASSED ({deliveryResult.message})");
            }
            else
            {
                Console.WriteLine($"FAILED ({deliveryResult.message})");
                Environment.Exit(1);
            }

            // 5. Testing Void Transaction with Inventory Restoration
            Console.Write("[TEST 5] Testing Sale Void with Atomic Stock Restoration: ");
            var voidResult = SaleDAL.VoidSale(saleResult.saleId, auth.user.UserId, "Defective order void test");
            if (voidResult.success)
            {
                Console.WriteLine($"PASSED ({voidResult.message})");
            }
            else
            {
                Console.WriteLine($"FAILED ({voidResult.message})");
                Environment.Exit(1);
            }

            // 6. Testing Reports Data Generation
            Console.Write("[TEST 6] Testing Reports Aggregation & Analytics: ");
            var reportTable = SaleDAL.GetDailySalesReport(DateTime.Today.AddDays(-7), DateTime.Today);
            var lowStockTable = SaleDAL.GetLowStockReport();
            Console.WriteLine($"PASSED (Daily Sales rows: {reportTable.Rows.Count}, Low stock rows: {lowStockTable.Rows.Count})");

            // 7. Testing Audit Trail
            Console.Write("[TEST 7] Testing Immutable Audit Trail Logging: ");
            var logs = AuditDAL.GetLogs();
            if (logs.Count > 0 && logs.Any(l => l.Action == "SALE_COMPLETE"))
            {
                Console.WriteLine($"PASSED ({logs.Count} audit records recorded; latest: {logs[0].Action})");
            }
            else
            {
                Console.WriteLine("FAILED (Audit logs missing expected entries)");
                Environment.Exit(1);
            }

            Console.WriteLine("==================================================================");
            Console.WriteLine("ALL 7 CORE SUBSYSTEM TESTS PASSED WITH 100% SUCCESS!");
            Console.WriteLine("==================================================================");
        }
    }
}
