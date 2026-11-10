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
    public class PosTerminalForm : Form
    {
        private List<Product> _allProducts = new();
        private List<Category> _allCategories = new();
        private readonly List<SaleDetail> _cartItems = new();

        // UI Controls - Catalog Panel
        private FlowLayoutPanel pnlCategoryTabs = null!;
        private TextBox txtSearchCatalog = null!;
        private NumericUpDown numAddQty = null!;
        private FlowLayoutPanel pnlProductGrid = null!;

        // UI Controls - Checkout Panel
        private DataGridView dgvCart = null!;
        private TextBox txtCustomerName = null!;
        private Label lblCartStats = null!;
        private Label lblSubtotalVal = null!;
        private Label lblGrandTotalVal = null!;
        private TextBox txtDiscount = null!;
        private TextBox txtAmountTendered = null!;
        private Label lblChangeDueVal = null!;
        private ComboBox cboPaymentMethod = null!;
        private ModernButton btnCharge = null!;
        private ModernButton btnClearCart = null!;
        private ModernButton btnVoidHistory = null!;

        private int _selectedCategoryId = 0;

        public PosTerminalForm()
        {
            if (!UserSession.CanAccessPos)
            {
                MessageBox.Show("Access Denied: Your account role does not have permission to access the POS Terminal.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "OmniPour POS Terminal - Cashier Counter";
            Size = new Size(1280, 800);
            MinimumSize = new Size(1100, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;
            KeyPreview = true;
            KeyDown += PosTerminalForm_KeyDown;

            // Header Bar
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = ModernTheme.ObsidianDark,
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblHeaderTitle = new Label
            {
                Text = "POS CASHIER COUNTER",
                Font = ModernTheme.FontSubTitle,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 14)
            };

            var lblCashierBadge = new Label
            {
                Text = $"Cashier: {UserSession.CurrentUser?.FullName ?? "Staff"} ({UserSession.CurrentUser?.RoleName})  •  Shortcuts: [F2] Search | [F3] Qty | [F12] Charge",
                Font = ModernTheme.FontSmall,
                ForeColor = ModernTheme.AmberAccent,
                AutoSize = true,
                Location = new Point(20, 36)
            };

            btnVoidHistory = new ModernButton
            {
                Text = "📋 Sales History / Void",
                NormalColor = Color.FromArgb(51, 65, 85),
                HoverColor = Color.FromArgb(71, 85, 105),
                Width = 175,
                Height = 38,
                Location = new Point(Width - 225, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnVoidHistory.Click += BtnVoidHistory_Click;

            topBar.Controls.AddRange(new Control[] { lblHeaderTitle, lblCashierBadge, btnVoidHistory });

            // Main Split: Left Catalog (Fill), Right Cart/Checkout (460px)
            var pnlCheckout = CreateCheckoutPanel();
            var pnlCatalog = CreateCatalogPanel();

            Controls.Add(pnlCatalog);
            Controls.Add(pnlCheckout);
            Controls.Add(topBar);
        }

        private Panel CreateCatalogPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.CanvasBackground,
                Padding = new Padding(20, 14, 12, 16)
            };

            // Search Bar & Quantity Header
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.Transparent
            };

            var lblSearch = new Label
            {
                Text = "Search:",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(0, 14),
                AutoSize = true
            };

            txtSearchCatalog = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(62, 10),
                Width = 240,
                PlaceholderText = "Search menu or SKU... (F2)"
            };
            txtSearchCatalog.TextChanged += (s, e) => RenderProductCards();

            // Quantity Control: Allows cashier to add custom quantity with 1 click!
            var lblQty = new Label
            {
                Text = "Add Qty:",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(318, 14),
                AutoSize = true
            };

            numAddQty = new NumericUpDown
            {
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(390, 10),
                Width = 65,
                Minimum = 1,
                Maximum = 999,
                Value = 1,
                TextAlign = HorizontalAlignment.Center
            };

            // Quick preset pills for quantity: 1, 2, 3, 5, 10
            var pnlQuickQty = new FlowLayoutPanel
            {
                Location = new Point(462, 8),
                Size = new Size(270, 36),
                WrapContents = false,
                BackColor = Color.Transparent
            };
            AddQuickQtyButton(pnlQuickQty, "1", 1);
            AddQuickQtyButton(pnlQuickQty, "2", 2);
            AddQuickQtyButton(pnlQuickQty, "3", 3);
            AddQuickQtyButton(pnlQuickQty, "5", 5);
            AddQuickQtyButton(pnlQuickQty, "10", 10);

            pnlFilter.Controls.AddRange(new Control[] { lblSearch, txtSearchCatalog, lblQty, numAddQty, pnlQuickQty });

            // Category Tabs Panel
            pnlCategoryTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 6)
            };

            // Product Cards Flow
            pnlProductGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };

            pnl.Controls.Add(pnlProductGrid);
            pnl.Controls.Add(pnlCategoryTabs);
            pnl.Controls.Add(pnlFilter);

            return pnl;
        }

        private void AddQuickQtyButton(FlowLayoutPanel pnl, string label, int val)
        {
            var btn = new Button
            {
                Text = label,
                Font = ModernTheme.FontSmallBold,
                Size = new Size(42, 30),
                Margin = new Padding(0, 2, 6, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ModernTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = ModernTheme.CardBorder;
            btn.Click += (s, e) => { numAddQty.Value = val; };
            pnl.Controls.Add(btn);
        }

        private Panel CreateCheckoutPanel()
        {
            var pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 460,
                BackColor = Color.White
            };

            // 1. Order Top Header
            var pnlCartHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 8)
            };

            var lblCartTitle = new Label
            {
                Text = "CURRENT ORDER",
                Font = ModernTheme.FontTitle,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 12)
            };

            var lblCust = new Label
            {
                Text = "Customer:",
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(16, 46),
                AutoSize = true
            };

            txtCustomerName = new TextBox
            {
                Text = "Walk-in Customer",
                Font = ModernTheme.FontBody,
                Location = new Point(88, 42),
                Width = 240
            };

            btnClearCart = new ModernButton
            {
                Text = "Clear",
                NormalColor = Color.FromArgb(226, 232, 240),
                HoverColor = Color.FromArgb(203, 213, 225),
                ForeColor = ModernTheme.RoseDanger,
                Width = 70,
                Height = 28,
                Location = new Point(345, 41),
                Font = ModernTheme.FontSmallBold
            };
            btnClearCart.Click += (s, e) =>
            {
                if (_cartItems.Count > 0)
                {
                    var confirm = MessageBox.Show("Clear all items from the current order?", "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirm == DialogResult.Yes)
                    {
                        _cartItems.Clear();
                        RefreshCartGrid();
                    }
                }
            };

            pnlCartHeader.Controls.AddRange(new Control[] { lblCartTitle, lblCust, txtCustomerName, btnClearCart });

            // 2. Bottom Checkout Summary & Charge Button
            var pnlBottomSection = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 350,
                BackColor = Color.White,
                Padding = new Padding(16, 6, 16, 14)
            };

            var pnlSummary = new ModernCard
            {
                Dock = DockStyle.Top,
                Height = 275,
                SurfaceColor = Color.FromArgb(248, 250, 252),
                BorderLineColor = ModernTheme.CardBorder,
                Padding = new Padding(14)
            };

            var lblSub = new Label { Text = "Subtotal:", Font = ModernTheme.FontBody, Location = new Point(14, 14), AutoSize = true };
            lblSubtotalVal = new Label { Text = "₱0.00", Font = ModernTheme.FontBodyBold, Location = new Point(300, 14), AutoSize = true };

            var lblDisc = new Label { Text = "Discount (₱):", Font = ModernTheme.FontBody, Location = new Point(14, 42), AutoSize = true };
            txtDiscount = new TextBox { Text = "0.00", Font = ModernTheme.FontBody, Location = new Point(300, 38), Width = 110 };
            txtDiscount.TextChanged += (s, e) => CalculateTotals();

            var lblPayMethod = new Label { Text = "Method:", Font = ModernTheme.FontBody, Location = new Point(14, 70), AutoSize = true };
            cboPaymentMethod = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ModernTheme.FontBody,
                Location = new Point(300, 66),
                Width = 110
            };
            cboPaymentMethod.Items.AddRange(new object[] { "Cash", "GCash", "Card" });
            cboPaymentMethod.SelectedIndex = 0;

            var lblGrand = new Label { Text = "GRAND TOTAL:", Font = ModernTheme.FontSubTitle, Location = new Point(14, 100), AutoSize = true };
            lblGrandTotalVal = new Label
            {
                Text = "₱0.00",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = ModernTheme.AmberAccentDark,
                Location = new Point(260, 96),
                AutoSize = true
            };

            var lblTender = new Label { Text = "Tendered (₱):", Font = ModernTheme.FontBodyBold, Location = new Point(14, 138), AutoSize = true };
            txtAmountTendered = new TextBox
            {
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(300, 134),
                Width = 110
            };
            txtAmountTendered.TextChanged += (s, e) => CalculateTotals();

            var pnlQuickCash = new FlowLayoutPanel
            {
                Location = new Point(14, 172),
                Size = new Size(396, 36),
                WrapContents = false
            };
            AddQuickCashButton(pnlQuickCash, "Exact", 0);
            AddQuickCashButton(pnlQuickCash, "₱100", 100);
            AddQuickCashButton(pnlQuickCash, "₱200", 200);
            AddQuickCashButton(pnlQuickCash, "₱500", 500);
            AddQuickCashButton(pnlQuickCash, "₱1,000", 1000);

            var lblChange = new Label { Text = "Change Due:", Font = ModernTheme.FontSubTitle, Location = new Point(14, 214), AutoSize = true };
            lblChangeDueVal = new Label
            {
                Text = "₱0.00",
                Font = ModernTheme.FontTitle,
                ForeColor = ModernTheme.EmeraldSuccess,
                Location = new Point(300, 212),
                AutoSize = true
            };

            pnlSummary.Controls.AddRange(new Control[] {
                lblSub, lblSubtotalVal, lblDisc, txtDiscount,
                lblPayMethod, cboPaymentMethod,
                lblGrand, lblGrandTotalVal,
                lblTender, txtAmountTendered, pnlQuickCash,
                lblChange, lblChangeDueVal
            });

            btnCharge = new ModernButton
            {
                Text = "CHARGE & PRINT SLIP (F12)",
                NormalColor = ModernTheme.AmberAccentDark,
                HoverColor = ModernTheme.AmberAccent,
                Dock = DockStyle.Bottom,
                Height = 48,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            btnCharge.Click += (s, e) => ProcessCheckout();

            pnlBottomSection.Controls.Add(btnCharge);
            pnlBottomSection.Controls.Add(pnlSummary);

            // 3. Middle Cart Grid & In-Cart Quantity Controls
            var pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 16, 6)
            };

            dgvCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            ModernTheme.StyleDataGridView(dgvCart);
            SetupCartGridColumns();

            // Quantity Modifier Toolbar directly below the order grid
            var pnlCartActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            var btnMinus = new Button
            {
                Text = "➖ -1",
                Font = ModernTheme.FontSmallBold,
                Size = new Size(54, 28),
                Location = new Point(0, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ModernTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnMinus.FlatAppearance.BorderColor = ModernTheme.CardBorder;
            btnMinus.Click += (s, e) => AdjustSelectedCartQuantity(-1);

            var btnPlus = new Button
            {
                Text = "➕ +1",
                Font = ModernTheme.FontSmallBold,
                Size = new Size(54, 28),
                Location = new Point(58, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ModernTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnPlus.FlatAppearance.BorderColor = ModernTheme.CardBorder;
            btnPlus.Click += (s, e) => AdjustSelectedCartQuantity(+1);

            var btnSetQty = new Button
            {
                Text = "✎ Set Qty",
                Font = ModernTheme.FontSmallBold,
                Size = new Size(82, 28),
                Location = new Point(116, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ModernTheme.AmberAccentDark,
                Cursor = Cursors.Hand
            };
            btnSetQty.FlatAppearance.BorderColor = ModernTheme.CardBorder;
            btnSetQty.Click += (s, e) => PromptSetSelectedQuantity();

            var btnRemove = new Button
            {
                Text = "🗑 Remove",
                Font = ModernTheme.FontSmallBold,
                Size = new Size(80, 28),
                Location = new Point(202, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = ModernTheme.RoseDanger,
                Cursor = Cursors.Hand
            };
            btnRemove.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnRemove.Click += (s, e) => RemoveSelectedCartItem();

            lblCartStats = new Label
            {
                Text = "0 items",
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(286, 8),
                Size = new Size(140, 20),
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false
            };

            pnlCartActions.Controls.AddRange(new Control[] { btnMinus, btnPlus, btnSetQty, btnRemove, lblCartStats });

            pnlGridWrapper.Controls.Add(dgvCart);
            pnlGridWrapper.Controls.Add(pnlCartActions);

            pnlRight.Controls.Add(pnlGridWrapper);
            pnlRight.Controls.Add(pnlBottomSection);
            pnlRight.Controls.Add(pnlCartHeader);

            return pnlRight;
        }

        private void AddQuickCashButton(FlowLayoutPanel pnl, string label, decimal val)
        {
            var btn = new Button
            {
                Text = label,
                Font = ModernTheme.FontSmallBold,
                Size = new Size(72, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ModernTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = ModernTheme.CardBorder;
            btn.Click += (s, e) =>
            {
                if (val == 0)
                {
                    decimal grand = GetCurrentGrandTotal();
                    txtAmountTendered.Text = grand.ToString("N2");
                }
                else
                {
                    txtAmountTendered.Text = val.ToString("N2");
                }
            };
            pnl.Controls.Add(btn);
        }

        private void SetupCartGridColumns()
        {
            dgvCart.AutoGenerateColumns = false;
            dgvCart.Columns.Clear();

            var colItem = new DataGridViewTextBoxColumn
            {
                HeaderText = "Item",
                DataPropertyName = "ProductName",
                ReadOnly = true,
                FillWeight = 46
            };

            var colQty = new DataGridViewTextBoxColumn
            {
                HeaderText = "Qty",
                DataPropertyName = "Quantity",
                ReadOnly = false,
                FillWeight = 16
            };

            var colPrice = new DataGridViewTextBoxColumn
            {
                HeaderText = "Price",
                DataPropertyName = "UnitPrice",
                ReadOnly = true,
                FillWeight = 19,
                DefaultCellStyle = { Format = "N2" }
            };

            var colSub = new DataGridViewTextBoxColumn
            {
                HeaderText = "Total",
                DataPropertyName = "Subtotal",
                ReadOnly = true,
                FillWeight = 19,
                DefaultCellStyle = { Format = "N2" }
            };

            dgvCart.Columns.AddRange(colItem, colQty, colPrice, colSub);
            dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCart.DataError += (s, e) => { e.ThrowException = false; };
            dgvCart.CellEndEdit += DgvCart_CellEndEdit;
            dgvCart.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) PromptSetSelectedQuantity(); };
        }

        private void DgvCart_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _cartItems.Count)
            {
                var row = dgvCart.Rows[e.RowIndex];
                if (int.TryParse(row.Cells[1].Value?.ToString(), out int newQty) && newQty > 0)
                {
                    _cartItems[e.RowIndex].Quantity = newQty;
                    _cartItems[e.RowIndex].Subtotal = _cartItems[e.RowIndex].UnitPrice * newQty;
                }
                else
                {
                    _cartItems.RemoveAt(e.RowIndex);
                }
                RefreshCartGrid();
            }
        }

        private SaleDetail? GetSelectedCartItem(out int index)
        {
            index = -1;
            if (dgvCart.CurrentRow != null && dgvCart.CurrentRow.Index >= 0 && dgvCart.CurrentRow.Index < _cartItems.Count)
            {
                index = dgvCart.CurrentRow.Index;
                return _cartItems[index];
            }
            return null;
        }

        private void AdjustSelectedCartQuantity(int delta)
        {
            var item = GetSelectedCartItem(out int idx);
            if (item == null) return;

            int newQty = item.Quantity + delta;
            if (newQty <= 0)
            {
                _cartItems.RemoveAt(idx);
            }
            else
            {
                item.Quantity = newQty;
                item.Subtotal = item.UnitPrice * newQty;
            }
            RefreshCartGrid();
            if (idx < dgvCart.Rows.Count)
            {
                dgvCart.CurrentCell = dgvCart.Rows[Math.Max(0, idx)].Cells[0];
            }
        }

        private void PromptSetSelectedQuantity()
        {
            var item = GetSelectedCartItem(out int idx);
            if (item == null)
            {
                MessageBox.Show("Please select an item in the cart to modify.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string input = PromptDialog.Show("Change Item Quantity", $"Enter new quantity for '{item.ProductName}':", item.Quantity.ToString());
            if (string.IsNullOrWhiteSpace(input)) return;

            if (int.TryParse(input.Trim(), out int newQty) && newQty > 0)
            {
                item.Quantity = newQty;
                item.Subtotal = item.UnitPrice * newQty;
                RefreshCartGrid();
                if (idx < dgvCart.Rows.Count)
                {
                    dgvCart.CurrentCell = dgvCart.Rows[idx].Cells[0];
                }
            }
            else if (newQty == 0)
            {
                _cartItems.RemoveAt(idx);
                RefreshCartGrid();
            }
            else
            {
                MessageBox.Show("Please enter a valid positive whole number for quantity.", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RemoveSelectedCartItem()
        {
            var item = GetSelectedCartItem(out int idx);
            if (item == null) return;

            _cartItems.RemoveAt(idx);
            RefreshCartGrid();
        }

        private void LoadData()
        {
            try
            {
                _allCategories = CategoryDAL.GetAll(activeOnly: true);
                _allProducts = ProductDAL.GetAll(activeOnly: true);

                RenderCategoryTabs();
                RenderProductCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load POS catalog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderCategoryTabs()
        {
            pnlCategoryTabs.Controls.Clear();

            var btnAll = CreateCategoryTabButton("All Items", 0);
            pnlCategoryTabs.Controls.Add(btnAll);

            foreach (var cat in _allCategories)
            {
                var btn = CreateCategoryTabButton(cat.CategoryName, cat.CategoryId);
                pnlCategoryTabs.Controls.Add(btn);
            }
        }

        private Button CreateCategoryTabButton(string name, int catId)
        {
            bool isSelected = _selectedCategoryId == catId;
            var btn = new Button
            {
                Text = name,
                UseMnemonic = false,
                AutoSize = true,
                Height = 36,
                Margin = new Padding(0, 0, 8, 4),
                Padding = new Padding(12, 4, 12, 4),
                FlatStyle = FlatStyle.Flat,
                Font = ModernTheme.FontBodyBold,
                BackColor = isSelected ? ModernTheme.AmberAccentDark : Color.White,
                ForeColor = isSelected ? Color.White : ModernTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = isSelected ? ModernTheme.AmberAccentDark : ModernTheme.CardBorder;
            btn.Click += (s, e) =>
            {
                _selectedCategoryId = catId;
                RenderCategoryTabs();
                RenderProductCards();
            };
            return btn;
        }

        private void RenderProductCards()
        {
            pnlProductGrid.Controls.Clear();
            string keyword = txtSearchCatalog.Text.Trim().ToLower();

            var filtered = _allProducts.Where(p =>
                (_selectedCategoryId == 0 || p.CategoryId == _selectedCategoryId) &&
                (string.IsNullOrEmpty(keyword) || p.ProductName.ToLower().Contains(keyword) || p.SKU.ToLower().Contains(keyword))
            ).ToList();

            if (filtered.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "No products found matching your search or category filter.",
                    Font = ModernTheme.FontBody,
                    ForeColor = ModernTheme.TextMuted,
                    AutoSize = true,
                    Padding = new Padding(20)
                };
                pnlProductGrid.Controls.Add(lblEmpty);
                return;
            }

            foreach (var prod in filtered)
            {
                var card = new ModernCard
                {
                    Size = new Size(185, 125),
                    Margin = new Padding(0, 0, 14, 14),
                    SurfaceColor = Color.White,
                    BorderLineColor = ModernTheme.CardBorder,
                    Cursor = Cursors.Hand
                };

                var lblName = new Label
                {
                    Text = prod.ProductName,
                    Font = ModernTheme.FontBodyBold,
                    ForeColor = ModernTheme.TextPrimary,
                    Location = new Point(12, 10),
                    Size = new Size(160, 42),
                    AutoEllipsis = true
                };

                var lblCat = new Label
                {
                    Text = prod.CategoryName,
                    Font = ModernTheme.FontSmall,
                    ForeColor = ModernTheme.TextMuted,
                    Location = new Point(12, 54),
                    AutoSize = true
                };

                var lblPrice = new Label
                {
                    Text = $"₱{prod.UnitPrice:N2}",
                    Font = ModernTheme.FontBodyBold,
                    ForeColor = ModernTheme.AmberAccentDark,
                    Location = new Point(12, 85),
                    AutoSize = true
                };

                var lblStock = new Label
                {
                    Text = prod.HasRecipe ? "Recipe Made" : $"{prod.StockQuantity} in stock",
                    Font = ModernTheme.FontSmallBold,
                    ForeColor = prod.IsLowStock && !prod.HasRecipe ? ModernTheme.RoseDanger : ModernTheme.TextSecondary,
                    Location = new Point(90, 88),
                    AutoSize = true
                };

                card.Controls.AddRange(new Control[] { lblName, lblCat, lblPrice, lblStock });

                // Click card to add product to cart with the specified quantity
                Action addToCart = () => AddProductToCart(prod);
                card.Click += (s, e) => addToCart();
                lblName.Click += (s, e) => addToCart();
                lblCat.Click += (s, e) => addToCart();
                lblPrice.Click += (s, e) => addToCart();
                lblStock.Click += (s, e) => addToCart();

                pnlProductGrid.Controls.Add(card);
            }
        }

        private void AddProductToCart(Product prod)
        {
            int addQuantity = (int)numAddQty.Value;
            if (addQuantity <= 0) addQuantity = 1;

            var existing = _cartItems.FirstOrDefault(x => x.ProductId == prod.ProductId);
            if (existing != null)
            {
                existing.Quantity += addQuantity;
                existing.Subtotal = existing.Quantity * existing.UnitPrice;
            }
            else
            {
                _cartItems.Add(new SaleDetail
                {
                    ProductId = prod.ProductId,
                    ProductName = prod.ProductName,
                    UnitPrice = prod.UnitPrice,
                    Quantity = addQuantity,
                    Subtotal = prod.UnitPrice * addQuantity
                });
            }
            RefreshCartGrid();
        }

        private void RefreshCartGrid()
        {
            dgvCart.DataSource = null;
            dgvCart.DataSource = _cartItems.ToList();
            CalculateTotals();

            // Update item stats
            int totalUnits = _cartItems.Sum(x => x.Quantity);
            lblCartStats.Text = $"{_cartItems.Count} item(s) • {totalUnits} unit(s)";
        }

        private decimal GetCurrentGrandTotal()
        {
            decimal subtotal = _cartItems.Sum(x => x.Subtotal);
            decimal.TryParse(txtDiscount.Text, out decimal discount);
            return Math.Max(0, subtotal - discount);
        }

        private void CalculateTotals()
        {
            decimal subtotal = _cartItems.Sum(x => x.Subtotal);
            decimal.TryParse(txtDiscount.Text, out decimal discount);
            decimal grandTotal = Math.Max(0, subtotal - discount);

            lblSubtotalVal.Text = $"₱{subtotal:N2}";
            lblGrandTotalVal.Text = $"₱{grandTotal:N2}";

            decimal.TryParse(txtAmountTendered.Text, out decimal tendered);
            decimal changeDue = Math.Max(0, tendered - grandTotal);
            lblChangeDueVal.Text = $"₱{changeDue:N2}";
        }

        private void ProcessCheckout()
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty. Please select products to sell.", "POS Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subtotal = _cartItems.Sum(x => x.Subtotal);
            if (!decimal.TryParse(txtDiscount.Text, out decimal discount) || discount < 0)
            {
                MessageBox.Show("Please enter a valid discount that is not negative.", "Payment Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDiscount.Focus();
                return;
            }

            decimal grandTotal = Math.Max(0, subtotal - discount);

            if (!decimal.TryParse(txtAmountTendered.Text, out decimal tendered) || tendered < 0 || tendered < grandTotal)
            {
                MessageBox.Show($"Amount tendered (₱{tendered:N2}) is insufficient. Total due is ₱{grandTotal:N2}.", "Payment Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAmountTendered.Focus();
                return;
            }

            decimal changeDue = tendered - grandTotal;

            var sale = new Sale
            {
                UserId = UserSession.CurrentUser?.UserId ?? 1,
                CashierName = UserSession.CurrentUser?.FullName ?? "Cashier",
                CustomerName = string.IsNullOrWhiteSpace(txtCustomerName.Text) ? "Walk-in Customer" : txtCustomerName.Text.Trim(),
                Subtotal = subtotal,
                DiscountAmount = discount,
                TaxAmount = 0,
                GrandTotal = grandTotal,
                AmountTendered = tendered,
                ChangeDue = changeDue,
                PaymentMethod = cboPaymentMethod.SelectedItem?.ToString() ?? "Cash",
                CreatedAt = DateTime.UtcNow,
                Items = new List<SaleDetail>(_cartItems)
            };

            btnCharge.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                // Execute Atomic Multi-Table Transaction via SqlTransaction
                var result = SaleDAL.ProcessSale(sale);
                if (!result.success)
                {
                    MessageBox.Show(result.message, "Transaction Rolled Back", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                sale.SaleId = result.saleId;
                sale.InvoiceNumber = result.invoiceNumber;

                // Show Printable Receipt Slip
                using var receiptForm = new ReceiptPrintPreviewForm(sale);
                receiptForm.ShowDialog(this);

                // Reset Cart & Inputs
                _cartItems.Clear();
                txtDiscount.Text = "0.00";
                txtAmountTendered.Text = "";
                txtCustomerName.Text = "Walk-in Customer";
                numAddQty.Value = 1;
                RefreshCartGrid();

                // Reload product stock
                _allProducts = ProductDAL.GetAll(activeOnly: true);
                RenderProductCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error during sale: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCharge.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void BtnVoidHistory_Click(object? sender, EventArgs e)
        {
            using var histForm = new SalesHistoryForm();
            histForm.ShowDialog(this);
            // Refresh stock in case a sale was voided
            _allProducts = ProductDAL.GetAll(activeOnly: true);
            RenderProductCards();
        }

        private void PosTerminalForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                ProcessCheckout();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                txtSearchCatalog.Focus();
                txtSearchCatalog.SelectAll();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                numAddQty.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && dgvCart.Focused)
            {
                RemoveSelectedCartItem();
                e.Handled = true;
            }
        }
    }
}
