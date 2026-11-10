using System;
using System.Collections.Generic;

namespace OmniPour.WinForms.Models
{
    public class Sale
    {
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = "Walk-in Customer";
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AmountTendered { get; set; }
        public decimal ChangeDue { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Paid";
        public DateTime CreatedAt { get; set; }
        public DateTime? VoidedAt { get; set; }
        public string VoidReason { get; set; } = string.Empty;
        public int? VoidedByUserId { get; set; }
        public string VoidedByName { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public List<SaleDetail> Items { get; set; } = new List<SaleDetail>();
    }

    public class SaleDetail
    {
        public int SaleDetailId { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class InventoryTransaction
    {
        public int InventoryTxId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = "Delivery"; // Delivery, Adjustment, Spoilage
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<InventoryTransactionDetail> Details { get; set; } = new List<InventoryTransactionDetail>();
    }

    public class InventoryTransactionDetail
    {
        public int DetailId { get; set; }
        public int InventoryTxId { get; set; }
        public int? IngredientId { get; set; }
        public int? ProductId { get; set; }
        public string ItemType { get; set; } = "Ingredient"; // Ingredient or Product
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class AuditLog
    {
        public int AuditId { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class DashboardStats
    {
        public decimal TodaySales { get; set; }
        public int TodayTransactionCount { get; set; }
        public int LowStockProductCount { get; set; }
        public int LowStockIngredientCount { get; set; }
        public decimal MonthlySales { get; set; }
        public int ActiveUsersCount { get; set; }
    }
}
