using System;

namespace OmniPour.WinForms.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public override string ToString() => RoleName;
    }

    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTime.UtcNow;
    }

    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public override string ToString() => CategoryName;
    }

    public class Ingredient
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal StockQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal CostPerUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public bool HasRecipe { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }

    public class ProductRecipe
    {
        public int ProductId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal QuantityRequired { get; set; }
    }
}
