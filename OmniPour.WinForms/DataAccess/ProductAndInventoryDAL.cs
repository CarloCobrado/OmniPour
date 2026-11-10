using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OmniPour.WinForms.Models;
using Microsoft.Data.SqlClient;

namespace OmniPour.WinForms.DataAccess
{
    public static class CategoryDAL
    {
        public static List<Category> GetAll(bool activeOnly = false)
        {
            var list = new List<Category>();
            string sql = "SELECT CategoryId, CategoryName, Description, IsActive, CreatedAt FROM dbo.Categories ";
            if (activeOnly) sql += " WHERE IsActive = 1 ";
            sql += " ORDER BY CategoryName ASC";

            var dt = DatabaseHelper.ExecuteQuery(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Category
                {
                    CategoryId = Convert.ToInt32(row["CategoryId"]),
                    CategoryName = row["CategoryName"].ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                });
            }
            return list;
        }

        public static int Create(Category cat)
        {
            const string sql = @"
                INSERT INTO dbo.Categories (CategoryName, Description, IsActive)
                OUTPUT INSERTED.CategoryId
                VALUES (@CategoryName, @Description, @IsActive)";

            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@CategoryName", cat.CategoryName.Trim()),
                new SqlParameter("@Description", (object?)cat.Description?.Trim() ?? DBNull.Value),
                new SqlParameter("@IsActive", cat.IsActive)
            }));
        }

        public static void Update(Category cat)
        {
            const string sql = "UPDATE dbo.Categories SET CategoryName = @CategoryName, Description = @Description, IsActive = @IsActive WHERE CategoryId = @Id";
            DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@CategoryName", cat.CategoryName.Trim()),
                new SqlParameter("@Description", (object?)cat.Description?.Trim() ?? DBNull.Value),
                new SqlParameter("@IsActive", cat.IsActive),
                new SqlParameter("@Id", cat.CategoryId)
            });
        }
    }

    public static class IngredientDAL
    {
        public static List<Ingredient> GetAll(bool activeOnly = false)
        {
            var list = new List<Ingredient>();
            string sql = "SELECT IngredientId, IngredientName, Unit, StockQuantity, ReorderLevel, CostPerUnit, IsActive, CreatedAt, RowVersion FROM dbo.Ingredients ";
            if (activeOnly) sql += " WHERE IsActive = 1 ";
            sql += " ORDER BY IngredientName ASC";

            var dt = DatabaseHelper.ExecuteQuery(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapIngredient(row));
            }
            return list;
        }

        public static Ingredient? GetById(int id)
        {
            const string sql = "SELECT IngredientId, IngredientName, Unit, StockQuantity, ReorderLevel, CostPerUnit, IsActive, CreatedAt, RowVersion FROM dbo.Ingredients WHERE IngredientId = @Id";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Id", id) });
            return dt.Rows.Count > 0 ? MapIngredient(dt.Rows[0]) : null;
        }

        public static int Create(Ingredient ing)
        {
            const string sql = @"
                INSERT INTO dbo.Ingredients (IngredientName, Unit, StockQuantity, ReorderLevel, CostPerUnit, IsActive)
                OUTPUT INSERTED.IngredientId
                VALUES (@Name, @Unit, @Stock, @Reorder, @Cost, @Active)";

            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@Name", ing.IngredientName.Trim()),
                new SqlParameter("@Unit", ing.Unit.Trim()),
                new SqlParameter("@Stock", ing.StockQuantity),
                new SqlParameter("@Reorder", ing.ReorderLevel),
                new SqlParameter("@Cost", ing.CostPerUnit),
                new SqlParameter("@Active", ing.IsActive)
            }));
        }

        public static bool Update(Ingredient ing)
        {
            // Optimistic concurrency check using RowVersion
            const string sql = @"
                UPDATE dbo.Ingredients
                SET IngredientName = @Name,
                    Unit = @Unit,
                    StockQuantity = @Stock,
                    ReorderLevel = @Reorder,
                    CostPerUnit = @Cost,
                    IsActive = @Active
                WHERE IngredientId = @Id AND RowVersion = @RowVersion";

            int rowsAffected = DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@Name", ing.IngredientName.Trim()),
                new SqlParameter("@Unit", ing.Unit.Trim()),
                new SqlParameter("@Stock", ing.StockQuantity),
                new SqlParameter("@Reorder", ing.ReorderLevel),
                new SqlParameter("@Cost", ing.CostPerUnit),
                new SqlParameter("@Active", ing.IsActive),
                new SqlParameter("@Id", ing.IngredientId),
                new SqlParameter("@RowVersion", ing.RowVersion)
            });

            return rowsAffected > 0;
        }

        private static Ingredient MapIngredient(DataRow row)
        {
            return new Ingredient
            {
                IngredientId = Convert.ToInt32(row["IngredientId"]),
                IngredientName = row["IngredientName"].ToString() ?? "",
                Unit = row["Unit"].ToString() ?? "",
                StockQuantity = Convert.ToDecimal(row["StockQuantity"]),
                ReorderLevel = Convert.ToDecimal(row["ReorderLevel"]),
                CostPerUnit = Convert.ToDecimal(row["CostPerUnit"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                RowVersion = (byte[])row["RowVersion"]
            };
        }
    }

    public static class ProductDAL
    {
        public static List<Product> GetAll(bool activeOnly = false, int? categoryId = null, string? searchKeyword = null)
        {
            var list = new List<Product>();
            string sql = @"
                SELECT p.ProductId, p.SKU, p.ProductName, p.CategoryId, c.CategoryName,
                       p.UnitPrice, p.StockQuantity, p.ReorderLevel, p.HasRecipe,
                       p.IsActive, p.CreatedAt, p.RowVersion
                FROM dbo.Products p
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                WHERE (1 = 1) ";

            var parameters = new List<SqlParameter>();

            if (activeOnly)
                sql += " AND p.IsActive = 1 ";

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                sql += " AND p.CategoryId = @CatId ";
                parameters.Add(new SqlParameter("@CatId", categoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                sql += " AND (p.ProductName LIKE @Kw OR p.SKU LIKE @Kw) ";
                parameters.Add(new SqlParameter("@Kw", $"%{searchKeyword.Trim()}%"));
            }

            sql += " ORDER BY c.CategoryName ASC, p.ProductName ASC";

            var dt = DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapProduct(row));
            }
            return list;
        }

        public static Product? GetById(int id)
        {
            const string sql = @"
                SELECT p.ProductId, p.SKU, p.ProductName, p.CategoryId, c.CategoryName,
                       p.UnitPrice, p.StockQuantity, p.ReorderLevel, p.HasRecipe,
                       p.IsActive, p.CreatedAt, p.RowVersion
                FROM dbo.Products p
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                WHERE p.ProductId = @Id";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Id", id) });
            return dt.Rows.Count > 0 ? MapProduct(dt.Rows[0]) : null;
        }

        public static int Create(Product p)
        {
            const string sql = @"
                INSERT INTO dbo.Products (SKU, ProductName, CategoryId, UnitPrice, StockQuantity, ReorderLevel, HasRecipe, IsActive)
                OUTPUT INSERTED.ProductId
                VALUES (@SKU, @Name, @CatId, @Price, @Stock, @Reorder, @HasRecipe, @Active)";

            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@SKU", p.SKU.Trim()),
                new SqlParameter("@Name", p.ProductName.Trim()),
                new SqlParameter("@CatId", p.CategoryId),
                new SqlParameter("@Price", p.UnitPrice),
                new SqlParameter("@Stock", p.StockQuantity),
                new SqlParameter("@Reorder", p.ReorderLevel),
                new SqlParameter("@HasRecipe", p.HasRecipe),
                new SqlParameter("@Active", p.IsActive)
            }));
        }

        public static bool Update(Product p)
        {
            // Optimistic concurrency check using RowVersion
            const string sql = @"
                UPDATE dbo.Products
                SET SKU = @SKU,
                    ProductName = @Name,
                    CategoryId = @CatId,
                    UnitPrice = @Price,
                    StockQuantity = @Stock,
                    ReorderLevel = @Reorder,
                    HasRecipe = @HasRecipe,
                    IsActive = @Active
                WHERE ProductId = @Id AND RowVersion = @RowVersion";

            int rows = DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@SKU", p.SKU.Trim()),
                new SqlParameter("@Name", p.ProductName.Trim()),
                new SqlParameter("@CatId", p.CategoryId),
                new SqlParameter("@Price", p.UnitPrice),
                new SqlParameter("@Stock", p.StockQuantity),
                new SqlParameter("@Reorder", p.ReorderLevel),
                new SqlParameter("@HasRecipe", p.HasRecipe),
                new SqlParameter("@Active", p.IsActive),
                new SqlParameter("@Id", p.ProductId),
                new SqlParameter("@RowVersion", p.RowVersion)
            });

            return rows > 0;
        }

        public static bool CanDeletePhysically(int productId)
        {
            const string sql = "SELECT COUNT(*) FROM dbo.SaleDetails WHERE ProductId = @Id";
            int count = Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@Id", productId) }));
            return count == 0;
        }

        private static Product MapProduct(DataRow row)
        {
            return new Product
            {
                ProductId = Convert.ToInt32(row["ProductId"]),
                SKU = row["SKU"].ToString() ?? "",
                ProductName = row["ProductName"].ToString() ?? "",
                CategoryId = Convert.ToInt32(row["CategoryId"]),
                CategoryName = row["CategoryName"].ToString() ?? "",
                UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
                StockQuantity = Convert.ToInt32(row["StockQuantity"]),
                ReorderLevel = Convert.ToInt32(row["ReorderLevel"]),
                HasRecipe = Convert.ToBoolean(row["HasRecipe"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                RowVersion = (byte[])row["RowVersion"]
            };
        }
    }

    public static class RecipeDAL
    {
        public static List<ProductRecipe> GetRecipeForProduct(int productId)
        {
            var list = new List<ProductRecipe>();
            const string sql = @"
                SELECT pr.ProductId, pr.IngredientId, i.IngredientName, i.Unit, pr.QuantityRequired
                FROM dbo.ProductRecipes pr
                INNER JOIN dbo.Ingredients i ON pr.IngredientId = i.IngredientId
                WHERE pr.ProductId = @ProductId";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ProductId", productId) });
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ProductRecipe
                {
                    ProductId = Convert.ToInt32(row["ProductId"]),
                    IngredientId = Convert.ToInt32(row["IngredientId"]),
                    IngredientName = row["IngredientName"].ToString() ?? "",
                    Unit = row["Unit"].ToString() ?? "",
                    QuantityRequired = Convert.ToDecimal(row["QuantityRequired"])
                });
            }
            return list;
        }

        public static void SaveRecipe(int productId, List<(int ingredientId, decimal quantity)> items)
        {
            if (items == null || items.Any(item => item.quantity <= 0))
                throw new ArgumentException("Recipe quantities must be greater than zero.", nameof(items));

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                using var delCmd = new SqlCommand("DELETE FROM dbo.ProductRecipes WHERE ProductId = @Pid", conn, trans);
                delCmd.Parameters.AddWithValue("@Pid", productId);
                delCmd.ExecuteNonQuery();

                foreach (var item in items)
                {
                    using var insCmd = new SqlCommand(@"
                        INSERT INTO dbo.ProductRecipes (ProductId, IngredientId, QuantityRequired)
                        VALUES (@Pid, @Iid, @Qty)", conn, trans);
                    insCmd.Parameters.AddWithValue("@Pid", productId);
                    insCmd.Parameters.AddWithValue("@Iid", item.ingredientId);
                    insCmd.Parameters.AddWithValue("@Qty", item.quantity);
                    insCmd.ExecuteNonQuery();
                }

                // Update HasRecipe flag on product
                using var updCmd = new SqlCommand("UPDATE dbo.Products SET HasRecipe = @HasRecipe WHERE ProductId = @Pid", conn, trans);
                updCmd.Parameters.AddWithValue("@HasRecipe", items.Count > 0);
                updCmd.Parameters.AddWithValue("@Pid", productId);
                updCmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}
