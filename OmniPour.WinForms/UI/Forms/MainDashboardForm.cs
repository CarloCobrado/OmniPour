using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OmniPour.WinForms.BusinessLogic;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    public class MainDashboardForm : Form
    {
        // Core Shell Panels
        private Panel pnlSidebar = null!;
        private Panel pnlTopBar = null!;
        private Panel pnlContent = null!;

        // Single-Window Embedded View Management
        private Panel pnlDashboardHome = null!;
        private Form? _activeEmbeddedForm = null;
        private readonly Dictionary<string, Button> _navButtons = new(StringComparer.OrdinalIgnoreCase);

        // Top Bar Controls
        private Label lblStore = null!;
        private Label lblClock = null!;
        private ModernButton btnTopBack = null!;
        private ModernButton btnTopRefresh = null!;
        private ModernButton btnTopPos = null!;
        private ModernButton btnTopDashboardSwitch = null!;
        private System.Windows.Forms.Timer tmrClock = null!;
        private bool _showInventoryDashboard;

        // KPI Card Value Labels
        private Label lblKpiRevenueVal = null!;
        private Label lblKpiTxCountVal = null!;
        private Label lblKpiLowProdVal = null!;
        private Label lblKpiLowIngVal = null!;

        // Recent Activity Grid
        private DataGridView dgvRecentSales = null!;
        private DataGridView dgvInventoryStatus = null!;
        private Label lblInventoryProductsVal = null!;
        private Label lblInventoryIngredientsVal = null!;
        private Label lblInventoryLowStockVal = null!;
        private Label lblInventoryOutOfStockVal = null!;

        public MainDashboardForm()
        {
            _showInventoryDashboard = UserSession.HasRole("Inventory Staff");
            InitializeComponent();
            ApplyRolePermissions();
            ShowDashboardHome();
        }

        private void InitializeComponent()
        {
            Text = "OmniPour Suite - Enterprise Dashboard";
            Size = new Size(1366, 850);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            MinimumSize = new Size(1180, 720);

            // 1. Sidebar (Obsidian Dark)
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = ModernTheme.ObsidianDark
            };
            BuildSidebar();

            // 2. Top Header Bar (Height 85 eliminates all font cutoff at any DPI)
            pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White
            };
            BuildTopBar();

            // 3. Main Content Panel (Hosts either Dashboard Home or Embedded Child Views)
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.CanvasBackground,
                AutoScroll = true
            };

            // Build the Dashboard Home container
            pnlDashboardHome = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.CanvasBackground,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 24)
            };
            BuildDashboardHomeContent();
            pnlContent.Controls.Add(pnlDashboardHome);

            Controls.Add(pnlContent);
            Controls.Add(pnlTopBar);
            Controls.Add(pnlSidebar);

            // Live Clock Timer
            tmrClock = new System.Windows.Forms.Timer { Interval = 1000 };
            tmrClock.Tick += (s, e) =>
            {
                if (lblClock != null && !lblClock.IsDisposed)
                {
                    lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  •  hh:mm:ss tt");
                }
            };
            tmrClock.Start();
        }

        private void BuildSidebar()
        {
            pnlSidebar.Controls.Clear();

            // Brand Header
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = ModernTheme.ObsidianDarker,
                Padding = new Padding(20, 14, 20, 14)
            };

            var lblLogo = new Label
            {
                Text = "☕ OmniPour",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 14)
            };

            var lblSub = new Label
            {
                Text = "POS & INVENTORY SUITE",
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.AmberAccent,
                AutoSize = true,
                Location = new Point(22, 46)
            };
            pnlBrand.Controls.AddRange(new Control[] { lblLogo, lblSub });

            // User Profile Pill at Bottom
            var pnlProfile = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 78,
                BackColor = Color.FromArgb(10, 15, 29),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblUserName = new Label
            {
                Text = UserSession.CurrentUser?.FullName ?? "Staff Member",
                Font = ModernTheme.FontBodyBold,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(14, 14)
            };

            var lblRole = new Label
            {
                Text = $"Role: {UserSession.CurrentUser?.RoleName ?? "Staff"}",
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.AmberAccent,
                AutoSize = true,
                Location = new Point(14, 38)
            };

            var btnLogout = new ModernButton
            {
                Text = "Logout",
                NormalColor = Color.FromArgb(51, 65, 85),
                HoverColor = ModernTheme.RoseDanger,
                Width = 72,
                Height = 32,
                Location = new Point(174, 22),
                Font = ModernTheme.FontSmallBold
            };
            btnLogout.Click += (s, e) => PerformLogout();

            pnlProfile.Controls.AddRange(new Control[] { lblUserName, lblRole, btnLogout });

            // Navigation Menu Container
            var pnlNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.ObsidianDark,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12, 10, 12, 10)
            };

            _navButtons.Clear();

            // 1. Dashboard (Always available to all logged-in roles)
            AddSidebarNavButton(pnlNav, "Dashboard Overview", "📊 Dashboard Overview", () => ShowDashboardHome());

            // 2. POS Terminal (Cashier, Supervisor, Administrator) - Hidden from Inventory Staff
            if (UserSession.CanAccessPos)
            {
                AddSidebarNavButton(pnlNav, "POS Cashier Register", "🛒 POS Cashier Register", () => LoadEmbeddedView(new PosTerminalForm(), "POS Cashier Register", "POS Cashier Register"));
                AddSidebarNavButton(pnlNav, "Sales History / Void", "📋 Sales History / Void", () => LoadEmbeddedView(new SalesHistoryForm(), "Sales History / Void", "Sales History & Auditing"));
            }

            // 3. Inventory Deliveries & Restock (Inventory Staff, Supervisor, Administrator) - Hidden from Cashier
            if (UserSession.CanAccessInventory)
            {
                AddSidebarNavButton(pnlNav, "Stock Restock", "📦 Stock Restock (Deliveries)", () => LoadEmbeddedView(new StockDeliveryForm(), "Stock Restock", "Stock Receiving & Restock"));
                AddSidebarNavButton(pnlNav, "Ingredients Inventory", "🌿 Ingredients Inventory", () => LoadEmbeddedView(new IngredientManagementForm(), "Ingredients Inventory", "Raw Ingredients Inventory"));
            }

            // 4. Products & Categories (Inventory Staff, Supervisor, Administrator) - Hidden from Cashier
            if (UserSession.CanAccessProducts)
            {
                AddSidebarNavButton(pnlNav, "Product Master File", "☕ Product Master File", () => LoadEmbeddedView(new ProductManagementForm(), "Product Master File", "Product Master & Recipes"));
                AddSidebarNavButton(pnlNav, "Categories Master", "🏷 Categories Master", () => LoadEmbeddedView(new CategoryManagementForm(), "Categories Master", "Categories Master File"));
            }

            // 5. Reports (Supervisor, Administrator) - Hidden from Cashier and Inventory Staff
            if (UserSession.CanAccessReports)
            {
                AddSidebarNavButton(pnlNav, "Analytic Reports", "📈 Analytic Reports", () => LoadEmbeddedView(new ReportsForm(), "Analytic Reports", "Analytic Reports & BI"));
            }

            // 6. Security & Admin Tools (Administrator only)
            if (UserSession.CanAccessUsers)
            {
                AddSidebarNavButton(pnlNav, "User Accounts", "👥 User Accounts", () => LoadEmbeddedView(new UserManagementForm(), "User Accounts", "User Accounts & Security"));
            }

            if (UserSession.CanAccessAudit)
            {
                AddSidebarNavButton(pnlNav, "Audit Trail Explorer", "📜 Audit Trail Explorer", () => LoadEmbeddedView(new AuditTrailForm(), "Audit Trail Explorer", "System Audit Trail"));
            }

            if (UserSession.CanAccessMaintenance)
            {
                AddSidebarNavButton(pnlNav, "Database Maintenance", "🛡 Database Maintenance", () => LoadEmbeddedView(new DatabaseMaintenanceForm(), "Database Maintenance", "Database Maintenance & Backup"));
            }

            pnlSidebar.Controls.Add(pnlNav);
            pnlSidebar.Controls.Add(pnlProfile);
            pnlSidebar.Controls.Add(pnlBrand);
        }

        private void AddSidebarNavButton(FlowLayoutPanel pnl, string navKey, string label, Action onClick)
        {
            var btn = new Button
            {
                Text = "  " + label,
                Font = ModernTheme.FontBodyBold,
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(234, 38),
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ModernTheme.SidebarHover;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(51, 65, 85);

            btn.Click += (s, e) => onClick();

            _navButtons[navKey] = btn;
            pnl.Controls.Add(btn);
        }

        private void HighlightNavButton(string activeKey)
        {
            foreach (var kvp in _navButtons)
            {
                bool isActive = string.Equals(kvp.Key, activeKey, StringComparison.OrdinalIgnoreCase);
                kvp.Value.BackColor = isActive ? ModernTheme.SidebarHover : Color.Transparent;
                kvp.Value.ForeColor = isActive ? Color.White : Color.FromArgb(203, 213, 225);
            }
        }

        private void BuildTopBar()
        {
            pnlTopBar.Controls.Clear();
            pnlTopBar.Height = 85;
            pnlTopBar.BackColor = Color.White;

            // Left side: Store Title & Live Date/Time neatly stacked with guaranteed heights
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 520,
                BackColor = Color.Transparent
            };

            lblStore = new Label
            {
                Text = "📍 OmniPour Operations Terminal",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(24, 14),
                Size = new Size(490, 26),
                AutoSize = false
            };

            lblClock = new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  •  hh:mm:ss tt"),
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(24, 44),
                Size = new Size(490, 26),
                AutoSize = false
            };

            pnlLeft.Controls.AddRange(new Control[] { lblStore, lblClock });

            // Right side: Dedicated responsive container for actions
            var pnlRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 22, 24, 20)
            };

            btnTopRefresh = new ModernButton
            {
                Text = "🔄 Refresh",
                NormalColor = Color.FromArgb(51, 65, 85),
                HoverColor = Color.FromArgb(71, 85, 105),
                Width = 110,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Font = ModernTheme.FontSmallBold
            };
            btnTopRefresh.Click += (s, e) =>
            {
                if (_activeEmbeddedForm == null)
                    RefreshDashboardData();
                else
                    ReloadActiveModule();
            };

            btnTopBack = new ModernButton
            {
                Text = "← Dashboard Overview",
                NormalColor = Color.FromArgb(71, 85, 105),
                HoverColor = Color.FromArgb(100, 116, 139),
                Width = 190,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Font = ModernTheme.FontSmallBold,
                Visible = false
            };
            btnTopBack.Click += (s, e) => ShowDashboardHome();

            btnTopPos = new ModernButton
            {
                Text = "+ Open POS Register",
                NormalColor = ModernTheme.AmberAccentDark,
                HoverColor = ModernTheme.AmberAccent,
                Width = 180,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Font = ModernTheme.FontSmallBold,
                Visible = UserSession.CanAccessPos
            };
            btnTopPos.Click += (s, e) => LoadEmbeddedView(new PosTerminalForm(), "POS Cashier Register", "POS Cashier Register");

            btnTopDashboardSwitch = new ModernButton
            {
                Text = "📦 Inventory View",
                NormalColor = Color.FromArgb(51, 65, 85),
                HoverColor = Color.FromArgb(71, 85, 105),
                Width = 160,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Font = ModernTheme.FontSmallBold,
                Visible = UserSession.CanAccessPos && UserSession.CanAccessInventory
            };
            btnTopDashboardSwitch.Click += (s, e) => ToggleDashboardView();

            pnlRight.Controls.AddRange(new Control[] { btnTopRefresh, btnTopBack, btnTopPos, btnTopDashboardSwitch });

            pnlTopBar.Controls.Add(pnlLeft);
            pnlTopBar.Controls.Add(pnlRight);
        }

        private void BuildDashboardHomeContent()
        {
            pnlDashboardHome.Controls.Clear();

            if (_showInventoryDashboard)
            {
                BuildInventoryDashboardContent();
                return;
            }

            var lblSectionTitle = new Label
            {
                Text = "Executive Summary & Operational KPI Indicators",
                Font = ModernTheme.FontTitle,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 35,
                AutoSize = false
            };

            // Responsive 4-Column TableLayoutPanel for KPI Cards
            var tblKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 145,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var cardRevenue = CreateKpiCard("TODAY'S REVENUE", "₱0.00", "Total sales processed today", ModernTheme.AmberAccentDark, out lblKpiRevenueVal);
            var cardTx = CreateKpiCard("COMPLETED SALES", "0 orders", "Recorded in system today", ModernTheme.IndigoInfo, out lblKpiTxCountVal);
            var cardLowProd = CreateKpiCard("LOW-STOCK PRODUCTS", "0 items", "Ready menu items near zero", ModernTheme.RoseDanger, out lblKpiLowProdVal);
            var cardLowIng = CreateKpiCard("LOW INGREDIENTS", "0 alerts", "Raw materials below threshold", ModernTheme.RoseDanger, out lblKpiLowIngVal);

            cardRevenue.Dock = DockStyle.Fill;
            cardTx.Dock = DockStyle.Fill;
            cardLowProd.Dock = DockStyle.Fill;
            cardLowIng.Dock = DockStyle.Fill;

            cardRevenue.Margin = new Padding(0, 0, 8, 0);
            cardTx.Margin = new Padding(4, 0, 4, 0);
            cardLowProd.Margin = new Padding(4, 0, 4, 0);
            cardLowIng.Margin = new Padding(8, 0, 0, 0);

            tblKpis.Controls.Add(cardRevenue, 0, 0);
            tblKpis.Controls.Add(cardTx, 1, 0);
            tblKpis.Controls.Add(cardLowProd, 2, 0);
            tblKpis.Controls.Add(cardLowIng, 3, 0);

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };

            // Recent Transactions Grid Card (Fills remaining vertical space)
            var pnlGridContainer = new ModernCard
            {
                Dock = DockStyle.Fill,
                SurfaceColor = Color.White,
                Padding = new Padding(20)
            };

            var lblGridTitle = new Label
            {
                Text = "Recent Transaction Activity (Live Stream)",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 35,
                AutoSize = false
            };

            dgvRecentSales = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoGenerateColumns = false
            };
            ModernTheme.StyleDataGridView(dgvRecentSales);
            SetupRecentColumns();

            pnlGridContainer.Controls.Add(dgvRecentSales);
            pnlGridContainer.Controls.Add(lblGridTitle);

            // Add controls in reverse dock order
            pnlDashboardHome.Controls.Add(pnlGridContainer);
            pnlDashboardHome.Controls.Add(pnlSpacer);
            pnlDashboardHome.Controls.Add(tblKpis);
            pnlDashboardHome.Controls.Add(lblSectionTitle);
        }

        private void BuildInventoryDashboardContent()
        {
            var lblSectionTitle = new Label
            {
                Text = "Inventory Status & Reorder Monitor",
                Font = ModernTheme.FontTitle,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 35,
                AutoSize = false
            };

            var tblKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 145,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            for (int i = 0; i < 4; i++)
                tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var cards = new[]
            {
                CreateKpiCard("ACTIVE PRODUCTS", "0 items", "Ready-to-sell products", ModernTheme.IndigoInfo, out lblInventoryProductsVal),
                CreateKpiCard("ACTIVE INGREDIENTS", "0 items", "Raw materials tracked", ModernTheme.AmberAccentDark, out lblInventoryIngredientsVal),
                CreateKpiCard("LOW-STOCK ITEMS", "0 items", "At or below reorder level", ModernTheme.RoseDanger, out lblInventoryLowStockVal),
                CreateKpiCard("OUT OF STOCK", "0 items", "Immediate attention required", ModernTheme.RoseDanger, out lblInventoryOutOfStockVal)
            };

            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Dock = DockStyle.Fill;
                cards[i].Margin = new Padding(i == 0 ? 0 : 4, 0, i == cards.Length - 1 ? 0 : 4, 0);
                tblKpis.Controls.Add(cards[i], i, 0);
            }

            var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
            var pnlGridContainer = new ModernCard
            {
                Dock = DockStyle.Fill,
                SurfaceColor = Color.White,
                Padding = new Padding(20)
            };
            var lblGridTitle = new Label
            {
                Text = "Current Stock by Product and Ingredient",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 35,
                AutoSize = false
            };

            dgvInventoryStatus = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoGenerateColumns = false
            };
            ModernTheme.StyleDataGridView(dgvInventoryStatus);
            dgvInventoryStatus.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", DataPropertyName = "ItemType", FillWeight = 18 });
            dgvInventoryStatus.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Item", DataPropertyName = "ItemName", FillWeight = 30 });
            dgvInventoryStatus.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit", DataPropertyName = "Unit", FillWeight = 10 });
            dgvInventoryStatus.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Current Stock", DataPropertyName = "CurrentStock", FillWeight = 14, DefaultCellStyle = { Format = "N3" } });
            dgvInventoryStatus.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reorder Level", DataPropertyName = "ReorderLevel", FillWeight = 14, DefaultCellStyle = { Format = "N3" } });
            dgvInventoryStatus.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 14 });
            dgvInventoryStatus.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvInventoryStatus.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 5 && e.Value?.ToString() is string status && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = status == "IN STOCK" ? ModernTheme.EmeraldSuccess : ModernTheme.RoseDanger;
                    e.CellStyle.Font = ModernTheme.FontBodyBold;
                }
            };

            pnlGridContainer.Controls.Add(dgvInventoryStatus);
            pnlGridContainer.Controls.Add(lblGridTitle);
            pnlDashboardHome.Controls.Add(pnlGridContainer);
            pnlDashboardHome.Controls.Add(pnlSpacer);
            pnlDashboardHome.Controls.Add(tblKpis);
            pnlDashboardHome.Controls.Add(lblSectionTitle);
        }

        private ModernCard CreateKpiCard(string title, string initialVal, string subtext, Color accentColor, out Label valLabel)
        {
            var card = new ModernCard
            {
                SurfaceColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22,
                AutoSize = false
            };

            var lblSub = new Label
            {
                Text = subtext,
                Font = ModernTheme.FontSmall,
                ForeColor = ModernTheme.TextMuted,
                Dock = DockStyle.Bottom,
                Height = 22,
                AutoEllipsis = true,
                AutoSize = false
            };

            valLabel = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                AutoSize = false
            };

            card.Controls.Add(valLabel);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblTitle);
            return card;
        }

        private void SetupRecentColumns()
        {
            dgvRecentSales.AutoGenerateColumns = false;
            dgvRecentSales.Columns.Clear();

            var colInvoice = new DataGridViewTextBoxColumn { HeaderText = "Invoice #", DataPropertyName = "InvoiceNumber", FillWeight = 22 };
            var colTime = new DataGridViewTextBoxColumn { HeaderText = "Time", DataPropertyName = "CreatedAt", FillWeight = 20, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" } };
            var colCashier = new DataGridViewTextBoxColumn { HeaderText = "Cashier", DataPropertyName = "CashierName", FillWeight = 20 };
            var colCustomer = new DataGridViewTextBoxColumn { HeaderText = "Customer", DataPropertyName = "CustomerName", FillWeight = 20 };
            var colTotal = new DataGridViewTextBoxColumn { HeaderText = "Grand Total (₱)", DataPropertyName = "GrandTotal", FillWeight = 16, DefaultCellStyle = { Format = "N2" } };
            var colStatus = new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "PaymentStatus", FillWeight = 12 };

            dgvRecentSales.Columns.AddRange(colInvoice, colTime, colCashier, colCustomer, colTotal, colStatus);
            dgvRecentSales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvRecentSales.DataError += (s, e) => { e.ThrowException = false; };
            dgvRecentSales.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 5 && e.Value != null && e.CellStyle != null)
                {
                    string status = e.Value.ToString() ?? "";
                    if (status == "Paid")
                    {
                        e.CellStyle.ForeColor = ModernTheme.EmeraldSuccess;
                        e.CellStyle.Font = ModernTheme.FontBodyBold;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = ModernTheme.RoseDanger;
                        e.CellStyle.Font = ModernTheme.FontBodyBold;
                    }
                }
            };
        }

        // ====================================================================
        // SINGLE-WINDOW WORKFLOW: Show Dashboard Overview
        // ====================================================================
        public void ShowDashboardHome()
        {
            if (_activeEmbeddedForm != null)
            {
                pnlContent.Controls.Remove(_activeEmbeddedForm);
                _activeEmbeddedForm.Dispose();
                _activeEmbeddedForm = null;
            }

            pnlDashboardHome.Visible = true;
            pnlDashboardHome.BringToFront();

            lblStore.Text = _showInventoryDashboard
                ? "📦 OmniPour Inventory Dashboard"
                : "📍 OmniPour Operations Terminal";
            btnTopBack.Visible = false;
            btnTopRefresh.Visible = true;
            btnTopPos.Visible = UserSession.CanAccessPos;
            btnTopDashboardSwitch.Visible = UserSession.CanAccessPos && UserSession.CanAccessInventory;
            btnTopDashboardSwitch.Text = _showInventoryDashboard ? "🛒 Cashier View" : "📦 Inventory View";

            HighlightNavButton("Dashboard Overview");
            RefreshDashboardData();
        }

        // ====================================================================
        // SINGLE-WINDOW WORKFLOW: Embed Any Module Directly Into Main Canvas
        // ====================================================================
        public void LoadEmbeddedView(Form childView, string navKey, string breadcrumbTitle)
        {
            try
            {
                pnlDashboardHome.Visible = false;

                if (_activeEmbeddedForm != null)
                {
                    pnlContent.Controls.Remove(_activeEmbeddedForm);
                    _activeEmbeddedForm.Dispose();
                    _activeEmbeddedForm = null;
                }

                _activeEmbeddedForm = childView;
                childView.TopLevel = false;
                childView.FormBorderStyle = FormBorderStyle.None;
                childView.Dock = DockStyle.Fill;
                childView.AutoScroll = true;

                pnlContent.Controls.Add(childView);
                childView.Show();
                childView.BringToFront();

                lblStore.Text = $"📍 OmniPour • {breadcrumbTitle}";
                btnTopBack.Visible = true;
                btnTopRefresh.Visible = true;
                btnTopPos.Visible = false;
                btnTopDashboardSwitch.Visible = false;

                HighlightNavButton(navKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load module: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowDashboardHome();
            }
        }

        private void ReloadActiveModule()
        {
            // If on a child form, re-trigger its reload if possible, otherwise keep as is
            if (_activeEmbeddedForm == null)
            {
                RefreshDashboardData();
            }
        }

        private void RefreshDashboardData()
        {
            try
            {
                if (_showInventoryDashboard)
                {
                    var inventory = SaleDAL.GetInventoryStatusReport();
                    int productCount = 0;
                    int ingredientCount = 0;
                    int lowStockCount = 0;
                    int outOfStockCount = 0;

                    foreach (System.Data.DataRow row in inventory.Rows)
                    {
                        string itemType = row["ItemType"].ToString() ?? string.Empty;
                        string status = row["Status"].ToString() ?? string.Empty;
                        if (itemType == "Ready Product") productCount++;
                        if (itemType == "Raw Ingredient") ingredientCount++;
                        if (status == "LOW STOCK" || status == "OUT OF STOCK") lowStockCount++;
                        if (status == "OUT OF STOCK") outOfStockCount++;
                    }

                    lblInventoryProductsVal.Text = $"{productCount} items";
                    lblInventoryIngredientsVal.Text = $"{ingredientCount} items";
                    lblInventoryLowStockVal.Text = $"{lowStockCount} items";
                    lblInventoryOutOfStockVal.Text = $"{outOfStockCount} items";
                    dgvInventoryStatus.DataSource = inventory;
                    return;
                }

                var stats = SaleDAL.GetDashboardStats();
                lblKpiRevenueVal.Text = $"₱{stats.TodaySales:N2}";
                lblKpiTxCountVal.Text = $"{stats.TodayTransactionCount} orders";
                lblKpiLowProdVal.Text = $"{stats.LowStockProductCount} items";
                lblKpiLowIngVal.Text = $"{stats.LowStockIngredientCount} alerts";

                var recent = SaleDAL.GetSales(DateTime.Today.AddDays(-3), DateTime.Today);
                dgvRecentSales.DataSource = recent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load dashboard data: {ex.Message}", "Dashboard Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleDashboardView()
        {
            if (!UserSession.CanAccessPos || !UserSession.CanAccessInventory)
                return;

            _showInventoryDashboard = !_showInventoryDashboard;
            BuildDashboardHomeContent();
            ShowDashboardHome();
        }

        private void ApplyRolePermissions()
        {
            Text = $"OmniPour Suite - {UserSession.CurrentUser?.RoleName} Terminal ({UserSession.CurrentUser?.FullName})";
        }

        private void PerformLogout()
        {
            var confirm = MessageBox.Show("Are you sure you want to log out of the current session?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                if (_activeEmbeddedForm != null)
                {
                    pnlContent.Controls.Remove(_activeEmbeddedForm);
                    _activeEmbeddedForm.Dispose();
                    _activeEmbeddedForm = null;
                }

                AuthBLL.Logout();
                Close();
            }
        }
    }
}
