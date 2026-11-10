using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OmniPour.WinForms.BusinessLogic;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    // ========================================================================
    // 1. PRODUCT MANAGEMENT FORM
    // ========================================================================
    public class ProductManagementForm : Form
    {
        private DataGridView dgvProducts = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboCategoryFilter = null!;
        private CheckBox chkActiveOnly = null!;
        private ModernButton btnNew = null!;
        private ModernButton btnEdit = null!;
        private ModernButton btnToggleActive = null!;
        private ModernButton btnRecipe = null!;

        public ProductManagementForm()
        {
            if (!UserSession.CanAccessProducts)
            {
                MessageBox.Show("Access Denied: Your account role does not have permission to view or edit Product Master Files.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadCategories();
            LoadProducts();
        }

        private void InitializeComponent()
        {
            Text = "Product Master Management";
            Size = new Size(1100, 680);
            MinimumSize = new Size(1000, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            // Top Header Bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "BEVERAGE & PRODUCT MASTER FILE", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            // Filter & Action Toolbar
            var toolBar = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };

            var lblSearch = new Label { Text = "Search:", Location = new Point(20, 22), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtSearch = new TextBox { Location = new Point(75, 18), Width = 180, PlaceholderText = "Name or SKU..." };
            txtSearch.TextChanged += (s, e) => LoadProducts();

            var lblCat = new Label { Text = "Category:", Location = new Point(270, 22), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboCategoryFilter = new ComboBox { Location = new Point(335, 18), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cboCategoryFilter.SelectedIndexChanged += (s, e) => LoadProducts();

            chkActiveOnly = new CheckBox { Text = "Active Only", Location = new Point(510, 20), AutoSize = true, Checked = false };
            chkActiveOnly.CheckedChanged += (s, e) => LoadProducts();

            btnNew = new ModernButton { Text = "+ New Product", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 115, Height = 34, Location = new Point(620, 15), Font = ModernTheme.FontSmallBold };
            btnNew.Click += BtnNew_Click;

            btnEdit = new ModernButton { Text = "Edit", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 80, Height = 34, Location = new Point(745, 15), Font = ModernTheme.FontSmallBold };
            btnEdit.Click += BtnEdit_Click;

            btnRecipe = new ModernButton { Text = "Recipe Ingredients", NormalColor = ModernTheme.IndigoInfo, HoverColor = Color.FromArgb(79, 70, 229), Width = 140, Height = 34, Location = new Point(835, 15), Font = ModernTheme.FontSmallBold };
            btnRecipe.Click += BtnRecipe_Click;

            btnToggleActive = new ModernButton { Text = "Toggle Active", NormalColor = ModernTheme.RoseDanger, HoverColor = Color.FromArgb(220, 38, 38), Width = 100, Height = 34, Location = new Point(985, 15), Font = ModernTheme.FontSmallBold };
            btnToggleActive.Click += BtnToggleActive_Click;

            toolBar.Controls.AddRange(new Control[] {
                lblSearch, txtSearch, lblCat, cboCategoryFilter, chkActiveOnly,
                btnNew, btnEdit, btnRecipe, btnToggleActive
            });

            // Grid Panel
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            dgvProducts = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvProducts);
            SetupColumns();
            centerPanel.Controls.Add(dgvProducts);

            Controls.Add(centerPanel);
            Controls.Add(toolBar);
            Controls.Add(topBar);
        }

        private void SetupColumns()
        {
            dgvProducts.Columns.Clear();
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ProductId", Width = 50 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SKU", DataPropertyName = "SKU", Width = 110 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Product Name", DataPropertyName = "ProductName", Width = 220 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category", DataPropertyName = "CategoryName", Width = 160 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Price (₱)", DataPropertyName = "UnitPrice", Width = 100, DefaultCellStyle = { Format = "N2" } });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stock", DataPropertyName = "StockQuantity", Width = 80 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reorder", DataPropertyName = "ReorderLevel", Width = 80 });
            dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Has Recipe", DataPropertyName = "HasRecipe", Width = 90 });
            dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", DataPropertyName = "IsActive", Width = 70 });
        }

        private void LoadCategories()
        {
            var cats = CategoryDAL.GetAll();
            cboCategoryFilter.Items.Clear();
            cboCategoryFilter.Items.Add(new Category { CategoryId = 0, CategoryName = "All Categories" });
            foreach (var c in cats) cboCategoryFilter.Items.Add(c);
            cboCategoryFilter.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            try
            {
                int? catId = (cboCategoryFilter.SelectedItem as Category)?.CategoryId;
                var list = ProductDAL.GetAll(chkActiveOnly.Checked, catId, txtSearch.Text);
                dgvProducts.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading products: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Product? GetSelectedProduct()
        {
            if (dgvProducts.CurrentRow?.DataBoundItem is Product p) return p;
            return null;
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            using var dlg = new ProductEditDialog(null);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null)
            {
                MessageBox.Show("Please select a product to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new ProductEditDialog(p);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void BtnRecipe_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null)
            {
                MessageBox.Show("Please select a product to configure its recipe.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new RecipeManagementDialog(p);
            dlg.ShowDialog(this);
            LoadProducts();
        }

        private void BtnToggleActive_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null)
            {
                MessageBox.Show("Please select a product to toggle status.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string actionText = p.IsActive ? "deactivate (soft-delete)" : "reactivate";
            var confirm = MessageBox.Show($"Are you sure you want to {actionText} product '{p.ProductName}'?", "Confirm Action",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            p.IsActive = !p.IsActive;
            bool success = ProductDAL.Update(p);
            if (!success)
            {
                MessageBox.Show("Concurrency Warning: This product was modified by another user. Reloading fresh data...",
                    "Conflict Detected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "PRODUCT_TOGGLE", "Products", p.ProductId.ToString(),
                    $"Toggled product '{p.ProductName}' active state to {p.IsActive}.");
            }
            LoadProducts();
        }
    }

    // ========================================================================
    // 2. PRODUCT EDIT DIALOG
    // ========================================================================
    public class ProductEditDialog : Form
    {
        private readonly Product? _product;
        private TextBox txtSKU = null!;
        private TextBox txtName = null!;
        private ComboBox cboCategory = null!;
        private TextBox txtPrice = null!;
        private NumericUpDown numStock = null!;
        private NumericUpDown numReorder = null!;
        private CheckBox chkActive = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public ProductEditDialog(Product? product)
        {
            _product = product;
            InitializeComponent();
            PopulateData();
        }

        private void InitializeComponent()
        {
            Text = _product == null ? "Add New Product" : $"Edit Product - {_product.ProductName}";
            Size = new Size(500, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = ModernTheme.FontBody;

            int left = 30, fieldLeft = 160, y = 30, gap = 44;

            var lblSKU = new Label { Text = "SKU / Code:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtSKU = new TextBox { Location = new Point(fieldLeft, y), Width = 280, Font = new Font("Segoe UI", 10F) };

            y += gap;
            var lblName = new Label { Text = "Product Name:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtName = new TextBox { Location = new Point(fieldLeft, y), Width = 280, Font = new Font("Segoe UI", 10F) };

            y += gap;
            var lblCat = new Label { Text = "Category:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            cboCategory = new ComboBox { Location = new Point(fieldLeft, y), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };

            y += gap;
            var lblPrice = new Label { Text = "Unit Price (₱):", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtPrice = new TextBox { Location = new Point(fieldLeft, y), Width = 150, Font = new Font("Segoe UI", 10F) };

            y += gap;
            var lblStock = new Label { Text = "Ready Stock:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            numStock = new NumericUpDown { Location = new Point(fieldLeft, y), Width = 150, Maximum = 999999, Font = new Font("Segoe UI", 10F) };

            y += gap;
            var lblReorder = new Label { Text = "Reorder Level:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            numReorder = new NumericUpDown { Location = new Point(fieldLeft, y), Width = 150, Maximum = 999999, Value = 5, Font = new Font("Segoe UI", 10F) };

            y += gap;
            chkActive = new CheckBox { Text = "Active & Available for Sale", Location = new Point(fieldLeft, y), AutoSize = true, Checked = true, Font = ModernTheme.FontBodyBold };

            y += 55;
            btnSave = new ModernButton { Text = "Save Product", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 130, Height = 38, Location = new Point(160, y) };
            btnSave.Click += BtnSave_Click;

            btnCancel = new ModernButton { Text = "Cancel", NormalColor = Color.FromArgb(100, 116, 139), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 38, Location = new Point(300, y) };
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                lblSKU, txtSKU, lblName, txtName, lblCat, cboCategory,
                lblPrice, txtPrice, lblStock, numStock, lblReorder, numReorder,
                chkActive, btnSave, btnCancel
            });
        }

        private void PopulateData()
        {
            var cats = CategoryDAL.GetAll(activeOnly: false);
            cboCategory.DataSource = cats;
            cboCategory.DisplayMember = "CategoryName";
            cboCategory.ValueMember = "CategoryId";

            if (_product != null)
            {
                txtSKU.Text = _product.SKU;
                txtName.Text = _product.ProductName;
                cboCategory.SelectedValue = _product.CategoryId;
                txtPrice.Text = _product.UnitPrice.ToString("N2");
                numStock.Value = Math.Max(0, _product.StockQuantity);
                numReorder.Value = Math.Max(0, _product.ReorderLevel);
                chkActive.Checked = _product.IsActive;
            }
            else
            {
                txtPrice.Text = "0.00";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid price greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return;
            }

            var p = _product ?? new Product();
            p.SKU = txtSKU.Text.Trim();
            p.ProductName = txtName.Text.Trim();
            p.CategoryId = (int)cboCategory.SelectedValue!;
            p.UnitPrice = price;
            p.StockQuantity = (int)numStock.Value;
            p.ReorderLevel = (int)numReorder.Value;
            p.IsActive = chkActive.Checked;

            var val = ProductBLL.ValidateProduct(p);
            if (!val.isValid)
            {
                MessageBox.Show(val.errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_product == null)
                {
                    ProductDAL.Create(p);
                    AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "PRODUCT_CREATE", "Products", p.SKU, $"Added product '{p.ProductName}'.");
                }
                else
                {
                    bool ok = ProductDAL.Update(p);
                    if (!ok)
                    {
                        MessageBox.Show("Concurrency Warning: This product was updated by another user simultaneously.", "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "PRODUCT_UPDATE", "Products", p.ProductId.ToString(), $"Updated product '{p.ProductName}'.");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save product: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ========================================================================
    // 3. RECIPE MANAGEMENT DIALOG
    // ========================================================================
    public class RecipeManagementDialog : Form
    {
        private readonly Product _product;
        private DataGridView dgvRecipe = null!;
        private ComboBox cboIngredients = null!;
        private TextBox txtQuantity = null!;
        private Label lblUnit = null!;
        private ModernButton btnAddIngredient = null!;
        private ModernButton btnRemoveIngredient = null!;
        private ModernButton btnSave = null!;
        private List<ProductRecipe> _recipeList = new();
        private List<Ingredient> _allIngredients = new();

        public RecipeManagementDialog(Product product)
        {
            _product = product ?? throw new ArgumentNullException(nameof(product));
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = $"Recipe Specification - {_product.ProductName}";
            Size = new Size(680, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = ModernTheme.FontBody;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(16, 12, 16, 12) };
            var lblTitle = new Label { Text = $"RECIPE SPECIFICATION: {_product.ProductName.ToUpper()}", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(16, 16) };
            topPanel.Controls.Add(lblTitle);

            // Add Ingredient Panel
            var pnlAdd = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(16) };

            var lblIng = new Label { Text = "Raw Ingredient:", Location = new Point(16, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboIngredients = new ComboBox { Location = new Point(115, 16), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboIngredients.SelectedIndexChanged += CboIngredients_SelectedIndexChanged;

            var lblQty = new Label { Text = "Qty Required:", Location = new Point(310, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtQuantity = new TextBox { Location = new Point(395, 16), Width = 65 };
            lblUnit = new Label { Text = "units", Location = new Point(465, 20), AutoSize = true, Font = ModernTheme.FontSmall };

            btnAddIngredient = new ModernButton { Text = "+ Add", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 70, Height = 30, Location = new Point(515, 14), Font = ModernTheme.FontSmallBold };
            btnAddIngredient.Click += BtnAddIngredient_Click;

            btnRemoveIngredient = new ModernButton { Text = "Remove", NormalColor = ModernTheme.RoseDanger, HoverColor = Color.FromArgb(220, 38, 38), Width = 75, Height = 30, Location = new Point(590, 14), Font = ModernTheme.FontSmallBold };
            btnRemoveIngredient.Click += BtnRemoveIngredient_Click;

            pnlAdd.Controls.AddRange(new Control[] { lblIng, cboIngredients, lblQty, txtQuantity, lblUnit, btnAddIngredient, btnRemoveIngredient });

            // Bottom Buttons
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };
            btnSave = new ModernButton { Text = "Save Recipe", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 140, Height = 40, Location = new Point(370, 10) };
            btnSave.Click += BtnSave_Click;
            var btnCancel = new ModernButton { Text = "Close", NormalColor = Color.FromArgb(100, 116, 139), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 40, Location = new Point(520, 10) };
            btnCancel.Click += (s, e) => Close();

            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);

            // Center Grid
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            dgvRecipe = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvRecipe);

            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "IngredientId", Width = 60 });
            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ingredient Name", DataPropertyName = "IngredientName", Width = 260 });
            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Amount Per Serving", DataPropertyName = "QuantityRequired", Width = 160, DefaultCellStyle = { Format = "N3" } });
            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit", DataPropertyName = "Unit", Width = 80 });

            centerPanel.Controls.Add(dgvRecipe);

            Controls.Add(centerPanel);
            Controls.Add(bottomPanel);
            Controls.Add(pnlAdd);
            Controls.Add(topPanel);
        }

        private void LoadData()
        {
            _allIngredients = IngredientDAL.GetAll(activeOnly: true);
            cboIngredients.DataSource = _allIngredients;
            cboIngredients.DisplayMember = "IngredientName";
            cboIngredients.ValueMember = "IngredientId";

            _recipeList = RecipeDAL.GetRecipeForProduct(_product.ProductId);
            dgvRecipe.DataSource = _recipeList;
        }

        private void CboIngredients_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboIngredients.SelectedItem is Ingredient ing)
            {
                lblUnit.Text = ing.Unit;
            }
        }

        private void BtnAddIngredient_Click(object? sender, EventArgs e)
        {
            if (cboIngredients.SelectedItem is not Ingredient ing) return;
            if (!decimal.TryParse(txtQuantity.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _recipeList.FirstOrDefault(x => x.IngredientId == ing.IngredientId);
            if (existing != null)
            {
                existing.QuantityRequired = qty;
            }
            else
            {
                _recipeList.Add(new ProductRecipe
                {
                    ProductId = _product.ProductId,
                    IngredientId = ing.IngredientId,
                    IngredientName = ing.IngredientName,
                    Unit = ing.Unit,
                    QuantityRequired = qty
                });
            }

            dgvRecipe.DataSource = null;
            dgvRecipe.DataSource = _recipeList;
            txtQuantity.Text = "";
        }

        private void BtnRemoveIngredient_Click(object? sender, EventArgs e)
        {
            if (dgvRecipe.CurrentRow?.DataBoundItem is ProductRecipe pr)
            {
                _recipeList.Remove(pr);
                dgvRecipe.DataSource = null;
                dgvRecipe.DataSource = _recipeList;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var tuples = _recipeList.Select(x => (x.IngredientId, x.QuantityRequired)).ToList();
                RecipeDAL.SaveRecipe(_product.ProductId, tuples);
                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "RECIPE_UPDATE", "ProductRecipes", _product.ProductId.ToString(),
                    $"Updated recipe for product '{_product.ProductName}' ({tuples.Count} ingredients).");

                MessageBox.Show("Recipe saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save recipe: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
