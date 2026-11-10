using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    public class StockDeliveryForm : Form
    {
        private List<Ingredient> _ingredients = new();
        private List<Product> _products = new();
        private readonly List<InventoryTransactionDetail> _details = new();

        private ComboBox cboType = null!;
        private TextBox txtSupplier = null!;
        private TextBox txtNotes = null!;
        private ComboBox cboItemType = null!;
        private ComboBox cboItems = null!;
        private TextBox txtQuantity = null!;
        private TextBox txtUnitCost = null!;
        private DataGridView dgvItems = null!;
        private Label lblTotalCostVal = null!;
        private ModernButton btnAddItem = null!;
        private ModernButton btnRemoveItem = null!;
        private ModernButton btnSubmitTx = null!;

        public StockDeliveryForm()
        {
            if (!UserSession.CanAccessInventory)
            {
                MessageBox.Show("Access Denied: Your account role does not have permission to access Inventory Restocking.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadMasterItems();
        }

        private void InitializeComponent()
        {
            Text = "Inventory Receiving & Stock Adjustments (Transaction Module 2)";
            Size = new Size(1000, 700);
            MinimumSize = new Size(980, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            // Header Bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "STOCK RECEIVING & INVENTORY RESTOCK (HEADER-DETAIL)", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            // Transaction Header Controls
            var pnlHeader = new ModernCard
            {
                Location = new Point(20, 75),
                Size = new Size(945, 120),
                SurfaceColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var lblType = new Label { Text = "Transaction Type:", Location = new Point(16, 16), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboType = new ComboBox { Location = new Point(16, 38), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboType.Items.AddRange(new object[] { "Delivery", "Adjustment", "Spoilage" });
            cboType.SelectedIndex = 0;

            var lblSup = new Label { Text = "Supplier / Origin:", Location = new Point(215, 16), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtSupplier = new TextBox { Location = new Point(215, 38), Width = 260, Text = "Manila Coffee Supply Co." };

            var lblNotes = new Label { Text = "Notes / Batch Reference:", Location = new Point(495, 16), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtNotes = new TextBox { Location = new Point(495, 38), Width = 420, Text = "Weekly raw ingredient restocking" };

            pnlHeader.Controls.AddRange(new Control[] { lblType, cboType, lblSup, txtSupplier, lblNotes, txtNotes });

            // Line Item Entry
            var pnlEntry = new ModernCard
            {
                Location = new Point(20, 210),
                Size = new Size(945, 90),
                SurfaceColor = Color.FromArgb(248, 250, 252),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var lblItmType = new Label { Text = "Item Classification:", Location = new Point(16, 14), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboItemType = new ComboBox { Location = new Point(16, 36), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cboItemType.Items.AddRange(new object[] { "Ingredient", "Product" });
            cboItemType.SelectedIndex = 0;
            cboItemType.SelectedIndexChanged += CboItemType_SelectedIndexChanged;

            var lblSelect = new Label { Text = "Select Item:", Location = new Point(175, 14), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboItems = new ComboBox { Location = new Point(175, 36), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cboItems.SelectedIndexChanged += CboItems_SelectedIndexChanged;

            var lblQty = new Label { Text = "Quantity:", Location = new Point(475, 14), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtQuantity = new TextBox { Location = new Point(475, 36), Width = 90 };

            var lblCost = new Label { Text = "Unit Cost (₱):", Location = new Point(585, 14), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtUnitCost = new TextBox { Location = new Point(585, 36), Width = 90 };

            btnAddItem = new ModernButton { Text = "+ Add Item", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 110, Height = 34, Location = new Point(695, 30), Font = ModernTheme.FontSmallBold };
            btnAddItem.Click += BtnAddItem_Click;

            btnRemoveItem = new ModernButton { Text = "Remove", NormalColor = ModernTheme.RoseDanger, HoverColor = Color.FromArgb(220, 38, 38), Width = 90, Height = 34, Location = new Point(815, 30), Font = ModernTheme.FontSmallBold };
            btnRemoveItem.Click += BtnRemoveItem_Click;

            pnlEntry.Controls.AddRange(new Control[] { lblItmType, cboItemType, lblSelect, cboItems, lblQty, txtQuantity, lblCost, txtUnitCost, btnAddItem, btnRemoveItem });

            // Grid for Details
            dgvItems = new DataGridView
            {
                Location = new Point(20, 315),
                Size = new Size(945, 240),
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoGenerateColumns = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            ModernTheme.StyleDataGridView(dgvItems);
            SetupDetailColumns();

            // Bottom Actions & Summary
            var pnlBottom = new Panel
            {
                Location = new Point(20, 570),
                Size = new Size(945, 60),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            var lblTotal = new Label { Text = "Total Batch Cost:", Font = ModernTheme.FontSubTitle, Location = new Point(10, 16), AutoSize = true };
            lblTotalCostVal = new Label { Text = "₱0.00", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = ModernTheme.AmberAccentDark, Location = new Point(160, 12), AutoSize = true };

            btnSubmitTx = new ModernButton
            {
                Text = "Commit Transaction (Atomic SqlTransaction)",
                NormalColor = ModernTheme.AmberAccentDark,
                HoverColor = ModernTheme.AmberAccent,
                Width = 350,
                Height = 44,
                Location = new Point(pnlBottom.Width - 360, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnSubmitTx.Click += BtnSubmitTx_Click;

            pnlBottom.Controls.AddRange(new Control[] { lblTotal, lblTotalCostVal, btnSubmitTx });

            Controls.AddRange(new Control[] { topBar, pnlHeader, pnlEntry, dgvItems, pnlBottom });
        }

        private void SetupDetailColumns()
        {
            dgvItems.Columns.Clear();
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", DataPropertyName = "ItemType", Width = 110 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Item Description", DataPropertyName = "ItemName", Width = 350 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = "Quantity", Width = 130, DefaultCellStyle = { Format = "N3" } });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit Cost (₱)", DataPropertyName = "UnitCost", Width = 140, DefaultCellStyle = { Format = "N2" } });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal (₱)", DataPropertyName = "Subtotal", Width = 160, DefaultCellStyle = { Format = "N2" } });
        }

        private void LoadMasterItems()
        {
            _ingredients = IngredientDAL.GetAll(activeOnly: true);
            _products = ProductDAL.GetAll(activeOnly: true);
            PopulateItemDropdown();
        }

        private void CboItemType_SelectedIndexChanged(object? sender, EventArgs e) => PopulateItemDropdown();

        private void PopulateItemDropdown()
        {
            cboItems.DataSource = null;
            if (cboItemType.SelectedItem?.ToString() == "Ingredient")
            {
                cboItems.DataSource = _ingredients;
                cboItems.DisplayMember = "IngredientName";
                cboItems.ValueMember = "IngredientId";
            }
            else
            {
                cboItems.DataSource = _products;
                cboItems.DisplayMember = "ProductName";
                cboItems.ValueMember = "ProductId";
            }
        }

        private void CboItems_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboItemType.SelectedItem?.ToString() == "Ingredient" && cboItems.SelectedItem is Ingredient ing)
            {
                txtUnitCost.Text = ing.CostPerUnit.ToString("N2");
            }
            else if (cboItems.SelectedItem is Product prod)
            {
                txtUnitCost.Text = prod.UnitPrice.ToString("N2");
            }
        }

        private void BtnAddItem_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtQuantity.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtUnitCost.Text, out decimal unitCost) || unitCost < 0)
            {
                MessageBox.Show("Please enter a valid unit cost that is not negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUnitCost.Focus();
                return;
            }

            string type = cboItemType.SelectedItem?.ToString() ?? "Ingredient";

            if (type == "Ingredient" && cboItems.SelectedItem is Ingredient ing)
            {
                _details.Add(new InventoryTransactionDetail
                {
                    ItemType = "Ingredient",
                    IngredientId = ing.IngredientId,
                    ItemName = $"{ing.IngredientName} ({ing.Unit})",
                    Quantity = qty,
                    UnitCost = unitCost,
                    Subtotal = qty * unitCost
                });
            }
            else if (type == "Product" && cboItems.SelectedItem is Product prod)
            {
                _details.Add(new InventoryTransactionDetail
                {
                    ItemType = "Product",
                    ProductId = prod.ProductId,
                    ItemName = prod.ProductName,
                    Quantity = qty,
                    UnitCost = unitCost,
                    Subtotal = qty * unitCost
                });
            }

            RefreshGrid();
            txtQuantity.Text = "";
        }

        private void BtnRemoveItem_Click(object? sender, EventArgs e)
        {
            if (dgvItems.CurrentRow?.DataBoundItem is InventoryTransactionDetail d)
            {
                _details.Remove(d);
                RefreshGrid();
            }
        }

        private void RefreshGrid()
        {
            dgvItems.DataSource = null;
            dgvItems.DataSource = _details.ToList();
            decimal total = _details.Sum(x => x.Subtotal);
            lblTotalCostVal.Text = $"₱{total:N2}";
        }

        private void BtnSubmitTx_Click(object? sender, EventArgs e)
        {
            if (_details.Count == 0)
            {
                MessageBox.Show("Please add at least one item to this inventory transaction.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Commit {_details.Count} items to inventory as '{cboType.SelectedItem}'?\n\nThis will execute an atomic multi-table transaction with automatic rollback protection.",
                "Confirm Inventory Transaction", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var tx = new InventoryTransaction
            {
                UserId = UserSession.CurrentUser?.UserId ?? 1,
                TransactionType = cboType.SelectedItem?.ToString() ?? "Delivery",
                SupplierName = txtSupplier.Text.Trim(),
                TotalCost = _details.Sum(x => x.Subtotal),
                Notes = txtNotes.Text.Trim(),
                Details = new List<InventoryTransactionDetail>(_details)
            };

            btnSubmitTx.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var result = InventoryTransactionDAL.ProcessTransaction(tx);
                if (result.success)
                {
                    MessageBox.Show(result.message, "Transaction Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _details.Clear();
                    RefreshGrid();
                    Close();
                }
                else
                {
                    MessageBox.Show(result.message, "Transaction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                btnSubmitTx.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }
}
