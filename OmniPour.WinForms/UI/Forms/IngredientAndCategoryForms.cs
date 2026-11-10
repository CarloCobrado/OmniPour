using System;
using System.Drawing;
using System.Windows.Forms;
using OmniPour.WinForms.BusinessLogic;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    // ========================================================================
    // INGREDIENT INVENTORY MANAGEMENT FORM
    // ========================================================================
    public class IngredientManagementForm : Form
    {
        private DataGridView dgvIngredients = null!;
        private CheckBox chkActiveOnly = null!;
        private ModernButton btnNew = null!;
        private ModernButton btnEdit = null!;
        private ModernButton btnToggleActive = null!;

        public IngredientManagementForm()
        {
            if (!UserSession.CanAccessInventory)
            {
                MessageBox.Show("Access Denied: Your account role does not have permission to view or manage Raw Ingredients.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadIngredients();
        }

        private void InitializeComponent()
        {
            Text = "Ingredient Inventory & Raw Materials";
            Size = new Size(1000, 650);
            MinimumSize = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            // Header Bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "RAW INGREDIENT INVENTORY & REORDER MONITOR", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            // Action Toolbar
            var toolBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };

            chkActiveOnly = new CheckBox { Text = "Active Only", Location = new Point(20, 20), AutoSize = true, Checked = false };
            chkActiveOnly.CheckedChanged += (s, e) => LoadIngredients();

            btnNew = new ModernButton { Text = "+ New Ingredient", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 140, Height = 34, Location = new Point(140, 13), Font = ModernTheme.FontSmallBold };
            btnNew.Click += BtnNew_Click;

            btnEdit = new ModernButton { Text = "Edit", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 80, Height = 34, Location = new Point(290, 13), Font = ModernTheme.FontSmallBold };
            btnEdit.Click += BtnEdit_Click;

            btnToggleActive = new ModernButton { Text = "Toggle Active", NormalColor = ModernTheme.RoseDanger, HoverColor = Color.FromArgb(220, 38, 38), Width = 110, Height = 34, Location = new Point(380, 13), Font = ModernTheme.FontSmallBold };
            btnToggleActive.Click += BtnToggleActive_Click;

            toolBar.Controls.AddRange(new Control[] { chkActiveOnly, btnNew, btnEdit, btnToggleActive });

            // Center Grid
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            dgvIngredients = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvIngredients);
            SetupColumns();
            centerPanel.Controls.Add(dgvIngredients);

            Controls.Add(centerPanel);
            Controls.Add(toolBar);
            Controls.Add(topBar);
        }

        private void SetupColumns()
        {
            dgvIngredients.Columns.Clear();
            dgvIngredients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "IngredientId", Width = 60 });
            dgvIngredients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ingredient Name", DataPropertyName = "IngredientName", Width = 260 });
            dgvIngredients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit", DataPropertyName = "Unit", Width = 80 });
            dgvIngredients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stock on Hand", DataPropertyName = "StockQuantity", Width = 140, DefaultCellStyle = { Format = "N3" } });
            dgvIngredients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reorder Level", DataPropertyName = "ReorderLevel", Width = 130, DefaultCellStyle = { Format = "N3" } });
            dgvIngredients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cost/Unit (₱)", DataPropertyName = "CostPerUnit", Width = 110, DefaultCellStyle = { Format = "N2" } });
            dgvIngredients.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", DataPropertyName = "IsActive", Width = 70 });

            dgvIngredients.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < dgvIngredients.Rows.Count)
                {
                    if (dgvIngredients.Rows[e.RowIndex].DataBoundItem is Ingredient ing && ing.IsLowStock)
                    {
                        if (e.ColumnIndex == 3 && e.CellStyle != null) // Stock on Hand
                        {
                            e.CellStyle.ForeColor = ModernTheme.RoseDanger;
                            e.CellStyle.Font = ModernTheme.FontBodyBold;
                        }
                    }
                }
            };
        }

        private void LoadIngredients()
        {
            try
            {
                var list = IngredientDAL.GetAll(chkActiveOnly.Checked);
                dgvIngredients.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading ingredients: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Ingredient? GetSelectedIngredient()
        {
            if (dgvIngredients.CurrentRow?.DataBoundItem is Ingredient ing) return ing;
            return null;
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            using var dlg = new IngredientEditDialog(null);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadIngredients();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var ing = GetSelectedIngredient();
            if (ing == null)
            {
                MessageBox.Show("Please select an ingredient to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new IngredientEditDialog(ing);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadIngredients();
        }

        private void BtnToggleActive_Click(object? sender, EventArgs e)
        {
            var ing = GetSelectedIngredient();
            if (ing == null)
            {
                MessageBox.Show("Please select an ingredient to toggle status.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string action = ing.IsActive ? "deactivate (soft-delete)" : "reactivate";
            if (MessageBox.Show($"Are you sure you want to {action} '{ing.IngredientName}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ing.IsActive = !ing.IsActive;
            bool ok = IngredientDAL.Update(ing);
            if (!ok)
            {
                MessageBox.Show("Concurrency Warning: This ingredient was updated by another user.", "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "INGREDIENT_TOGGLE", "Ingredients", ing.IngredientId.ToString(),
                    $"Toggled ingredient '{ing.IngredientName}' active state to {ing.IsActive}.");
            }
            LoadIngredients();
        }
    }

    public class IngredientEditDialog : Form
    {
        private readonly Ingredient? _ing;
        private TextBox txtName = null!;
        private ComboBox cboUnit = null!;
        private TextBox txtStock = null!;
        private TextBox txtReorder = null!;
        private TextBox txtCost = null!;
        private CheckBox chkActive = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public IngredientEditDialog(Ingredient? ing)
        {
            _ing = ing;
            InitializeComponent();
            PopulateData();
        }

        private void InitializeComponent()
        {
            Text = _ing == null ? "Add Raw Ingredient" : $"Edit Ingredient - {_ing.IngredientName}";
            Size = new Size(480, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = ModernTheme.FontBody;

            int left = 30, fieldLeft = 160, y = 30, gap = 44;

            var lblName = new Label { Text = "Ingredient Name:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtName = new TextBox { Location = new Point(fieldLeft, y), Width = 260 };

            y += gap;
            var lblUnit = new Label { Text = "Measurement Unit:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            cboUnit = new ComboBox { Location = new Point(fieldLeft, y), Width = 150, DropDownStyle = ComboBoxStyle.DropDown };
            cboUnit.Items.AddRange(new object[] { "g", "ml", "pcs", "shots", "oz", "kg", "L" });

            y += gap;
            var lblStock = new Label { Text = "Stock on Hand:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtStock = new TextBox { Location = new Point(fieldLeft, y), Width = 150 };

            y += gap;
            var lblReorder = new Label { Text = "Reorder Threshold:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtReorder = new TextBox { Location = new Point(fieldLeft, y), Width = 150 };

            y += gap;
            var lblCost = new Label { Text = "Cost per Unit (₱):", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtCost = new TextBox { Location = new Point(fieldLeft, y), Width = 150 };

            y += gap;
            chkActive = new CheckBox { Text = "Active & Available for Recipes", Location = new Point(fieldLeft, y), AutoSize = true, Checked = true, Font = ModernTheme.FontBodyBold };

            y += 50;
            btnSave = new ModernButton { Text = "Save Ingredient", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 140, Height = 38, Location = new Point(140, y) };
            btnSave.Click += BtnSave_Click;
            btnCancel = new ModernButton { Text = "Cancel", NormalColor = Color.FromArgb(100, 116, 139), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 38, Location = new Point(290, y) };
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                lblName, txtName, lblUnit, cboUnit,
                lblStock, txtStock, lblReorder, txtReorder,
                lblCost, txtCost, chkActive, btnSave, btnCancel
            });
        }

        private void PopulateData()
        {
            if (_ing != null)
            {
                txtName.Text = _ing.IngredientName;
                cboUnit.Text = _ing.Unit;
                txtStock.Text = _ing.StockQuantity.ToString("N3");
                txtReorder.Text = _ing.ReorderLevel.ToString("N3");
                txtCost.Text = _ing.CostPerUnit.ToString("N2");
                chkActive.Checked = _ing.IsActive;
            }
            else
            {
                cboUnit.Text = "g";
                txtStock.Text = "0.000";
                txtReorder.Text = "100.000";
                txtCost.Text = "0.00";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtStock.Text, out decimal stock) || stock < 0 ||
                !decimal.TryParse(txtReorder.Text, out decimal reorder) || reorder < 0 ||
                !decimal.TryParse(txtCost.Text, out decimal cost) || cost < 0)
            {
                MessageBox.Show("Please enter valid positive numbers for stock, reorder level, and cost.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ing = _ing ?? new Ingredient();
            ing.IngredientName = txtName.Text.Trim();
            ing.Unit = cboUnit.Text.Trim();
            ing.StockQuantity = stock;
            ing.ReorderLevel = reorder;
            ing.CostPerUnit = cost;
            ing.IsActive = chkActive.Checked;

            var val = ProductBLL.ValidateIngredient(ing);
            if (!val.isValid)
            {
                MessageBox.Show(val.errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_ing == null)
                {
                    IngredientDAL.Create(ing);
                    AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "INGREDIENT_CREATE", "Ingredients", ing.IngredientName, $"Added ingredient '{ing.IngredientName}'.");
                }
                else
                {
                    bool ok = IngredientDAL.Update(ing);
                    if (!ok)
                    {
                        MessageBox.Show("Concurrency Warning: This ingredient was modified by another user simultaneously.", "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "INGREDIENT_UPDATE", "Ingredients", ing.IngredientId.ToString(), $"Updated ingredient '{ing.IngredientName}'.");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save ingredient: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ========================================================================
    // CATEGORY MASTER MANAGEMENT FORM
    // ========================================================================
    public class CategoryManagementForm : Form
    {
        private DataGridView dgvCategories = null!;
        private TextBox txtCategoryName = null!;
        private TextBox txtDescription = null!;
        private ModernButton btnAdd = null!;
        private ModernButton btnUpdate = null!;

        public CategoryManagementForm()
        {
            if (!UserSession.CanAccessCategories)
            {
                MessageBox.Show("Access Denied: Your account role does not have permission to manage Categories.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadCategories();
        }

        private void InitializeComponent()
        {
            Text = "Category Master Management";
            Size = new Size(800, 560);
            MinimumSize = new Size(750, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            var topBar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(16, 12, 16, 12) };
            var lblTitle = new Label { Text = "BEVERAGE CATEGORIES MASTER", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(16, 14) };
            topBar.Controls.Add(lblTitle);

            // Editor Panel
            var pnlEdit = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(16) };
            var lblName = new Label { Text = "Category Name:", Location = new Point(16, 16), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtCategoryName = new TextBox { Location = new Point(16, 38), Width = 220 };

            var lblDesc = new Label { Text = "Description:", Location = new Point(255, 16), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtDescription = new TextBox { Location = new Point(255, 38), Width = 280 };

            btnAdd = new ModernButton { Text = "+ Add", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 85, Height = 32, Location = new Point(555, 34), Font = ModernTheme.FontSmallBold };
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = new ModernButton { Text = "Save Changes", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 110, Height = 32, Location = new Point(650, 34), Font = ModernTheme.FontSmallBold };
            btnUpdate.Click += BtnUpdate_Click;

            pnlEdit.Controls.AddRange(new Control[] { lblName, txtCategoryName, lblDesc, txtDescription, btnAdd, btnUpdate });

            // Center Grid
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            dgvCategories = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvCategories);

            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "CategoryId", Width = 60 });
            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category Name", DataPropertyName = "CategoryName", Width = 250 });
            dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Description", DataPropertyName = "Description", Width = 350 });
            dgvCategories.SelectionChanged += DgvCategories_SelectionChanged;

            centerPanel.Controls.Add(dgvCategories);

            Controls.Add(centerPanel);
            Controls.Add(pnlEdit);
            Controls.Add(topBar);
        }

        private void LoadCategories()
        {
            try
            {
                var list = CategoryDAL.GetAll();
                dgvCategories.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading categories: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvCategories_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvCategories.CurrentRow?.DataBoundItem is Category cat)
            {
                txtCategoryName.Text = cat.CategoryName;
                txtDescription.Text = cat.Description;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string name = txtCategoryName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var cat = new Category { CategoryName = name, Description = txtDescription.Text.Trim() };
                CategoryDAL.Create(cat);
                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "CATEGORY_CREATE", "Categories", name, $"Created category '{name}'.");
                txtCategoryName.Text = "";
                txtDescription.Text = "";
                LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add category: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (dgvCategories.CurrentRow?.DataBoundItem is not Category cat)
            {
                MessageBox.Show("Please select a category to update.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = txtCategoryName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                cat.CategoryName = name;
                cat.Description = txtDescription.Text.Trim();
                CategoryDAL.Update(cat);
                AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "USER", "CATEGORY_UPDATE", "Categories", cat.CategoryId.ToString(), $"Updated category '{name}'.");
                LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update category: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
