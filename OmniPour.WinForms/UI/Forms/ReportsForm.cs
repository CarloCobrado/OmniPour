using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    public class ReportsForm : Form
    {
        private TabControl tabReports = null!;
        private DataGridView dgvDailySales = null!;
        private DataGridView dgvSalesByProduct = null!;
        private DataGridView dgvLowStock = null!;

        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private ModernButton btnRefresh = null!;
        private ModernButton btnExportCsv = null!;
        private ModernButton btnPrint = null!;

        private Label lblTotalRevenue = null!;
        private Label lblTotalUnitsSold = null!;
        private PrintDocument printDoc = null!;

        public ReportsForm()
        {
            if (!UserSession.CanAccessReports)
            {
                MessageBox.Show("Access Denied: Only Supervisors or Administrators can view Analytic Reports.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadActiveReport();
        }

        private void InitializeComponent()
        {
            Text = "Business Intelligence & Transaction Reports";
            Size = new Size(1150, 720);
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            // Top Header Bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "ANALYTIC REPORTS & AUDIT SUMMARIES", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            // Filter Toolbar
            var filterBar = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };

            var lblFrom = new Label { Text = "Date Range From:", Location = new Point(20, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            dtpFrom = new DateTimePicker { Location = new Point(140, 16), Width = 130, Value = DateTime.Today.AddDays(-30) };

            var lblTo = new Label { Text = "To:", Location = new Point(285, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            dtpTo = new DateTimePicker { Location = new Point(315, 16), Width = 130, Value = DateTime.Today };

            btnRefresh = new ModernButton { Text = "🔄 Run Report", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 120, Height = 34, Location = new Point(460, 14), Font = ModernTheme.FontSmallBold };
            btnRefresh.Click += (s, e) => LoadActiveReport();

            btnExportCsv = new ModernButton { Text = "📥 Export to CSV", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 130, Height = 34, Location = new Point(590, 14), Font = ModernTheme.FontSmallBold };
            btnExportCsv.Click += BtnExportCsv_Click;

            btnPrint = new ModernButton { Text = "🖨 Print Report", NormalColor = ModernTheme.IndigoInfo, HoverColor = Color.FromArgb(79, 70, 229), Width = 120, Height = 34, Location = new Point(730, 14), Font = ModernTheme.FontSmallBold };
            btnPrint.Click += BtnPrint_Click;

            filterBar.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnRefresh, btnExportCsv, btnPrint });

            // Bottom Summary Bar
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };
            lblTotalRevenue = new Label { Text = "Filtered Revenue: ₱0.00", Font = ModernTheme.FontBodyBold, ForeColor = ModernTheme.AmberAccentDark, Location = new Point(20, 14), AutoSize = true };
            lblTotalUnitsSold = new Label { Text = "Units Sold: 0", Font = ModernTheme.FontBodyBold, ForeColor = ModernTheme.TextPrimary, Location = new Point(350, 14), AutoSize = true };
            bottomBar.Controls.AddRange(new Control[] { lblTotalRevenue, lblTotalUnitsSold });

            // Tab Control for 3 Distinct Reports
            tabReports = new TabControl { Dock = DockStyle.Fill, Font = ModernTheme.FontSubTitle };
            tabReports.SelectedIndexChanged += (s, e) => LoadActiveReport();

            // Tab 1: Daily Sales
            var tabDaily = new TabPage("Daily Sales Summary");
            dgvDailySales = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvDailySales);
            tabDaily.Controls.Add(dgvDailySales);

            // Tab 2: Sales by Product & Category
            var tabProd = new TabPage("Sales by Product & Category");
            dgvSalesByProduct = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvSalesByProduct);
            tabProd.Controls.Add(dgvSalesByProduct);

            // Tab 3: Low Stock Alerts
            var tabStock = new TabPage("Low-Stock & Reorder Alerts");
            dgvLowStock = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvLowStock);
            tabStock.Controls.Add(dgvLowStock);

            tabReports.TabPages.AddRange(new TabPage[] { tabDaily, tabProd, tabStock });

            Controls.Add(tabReports);
            Controls.Add(bottomBar);
            Controls.Add(filterBar);
            Controls.Add(topBar);

            printDoc = new PrintDocument();
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private void LoadActiveReport()
        {
            try
            {
                if (tabReports.SelectedIndex == 0)
                {
                    var dt = SaleDAL.GetDailySalesReport(dtpFrom.Value, dtpTo.Value);
                    dgvDailySales.DataSource = dt;

                    decimal totalSales = 0;
                    foreach (DataRow row in dt.Rows)
                        totalSales += Convert.ToDecimal(row["NetSales"]);

                    lblTotalRevenue.Text = $"Filtered Net Revenue: ₱{totalSales:N2}";
                    lblTotalUnitsSold.Text = $"Active Days: {dt.Rows.Count}";
                }
                else if (tabReports.SelectedIndex == 1)
                {
                    var dt = SaleDAL.GetSalesByProductReport(dtpFrom.Value, dtpTo.Value);
                    dgvSalesByProduct.DataSource = dt;

                    decimal totalRev = 0;
                    int units = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        totalRev += Convert.ToDecimal(row["TotalRevenue"]);
                        units += Convert.ToInt32(row["QuantitySold"]);
                    }

                    lblTotalRevenue.Text = $"Product Revenue: ₱{totalRev:N2}";
                    lblTotalUnitsSold.Text = $"Total Cups / Units Sold: {units:N0}";
                }
                else if (tabReports.SelectedIndex == 2)
                {
                    var dt = SaleDAL.GetLowStockReport();
                    dgvLowStock.DataSource = dt;

                    lblTotalRevenue.Text = $"Critical Low-Stock Items: {dt.Rows.Count}";
                    lblTotalUnitsSold.Text = "Attention Needed: Stock below reorder threshold";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridView GetCurrentGrid()
        {
            return tabReports.SelectedIndex switch
            {
                0 => dgvDailySales,
                1 => dgvSalesByProduct,
                _ => dgvLowStock
            };
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            var grid = GetCurrentGrid();
            if (grid.DataSource is not DataTable dt || dt.Rows.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"{tabReports.SelectedTab?.Text.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();

                // Headers
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    sb.Append("\"" + dt.Columns[i].ColumnName.Replace("\"", "\"\"") + "\"");
                    if (i < dt.Columns.Count - 1) sb.Append(",");
                }
                sb.AppendLine();

                // Rows
                foreach (DataRow row in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        sb.Append("\"" + row[i].ToString()?.Replace("\"", "\"\"") + "\"");
                        if (i < dt.Columns.Count - 1) sb.Append(",");
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Report successfully exported to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                using var dlg = new PrintDialog { Document = printDoc };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    printDoc.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var grid = GetCurrentGrid();
            if (grid.DataSource is not DataTable dt) return;

            using var titleFont = new Font("Segoe UI", 14F, FontStyle.Bold);
            using var headerFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 8.5F);

            var g = e.Graphics!;
            int y = 40;

            g.DrawString($"OMNIPOUR REPORT: {tabReports.SelectedTab?.Text.ToUpper()}", titleFont, Brushes.Black, 40, y);
            y += 28;
            g.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm} | Filter: {dtpFrom.Value:yyyy-MM-dd} to {dtpTo.Value:yyyy-MM-dd}", bodyFont, Brushes.DarkSlateGray, 40, y);
            y += 30;

            int x = 40;
            int colWidth = Math.Max(70, 700 / Math.Max(1, dt.Columns.Count));

            // Draw Column Headers
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                g.DrawString(dt.Columns[i].ColumnName, headerFont, Brushes.Black, x + (i * colWidth), y);
            }
            y += 20;
            g.DrawLine(Pens.Black, 40, y, 40 + (dt.Columns.Count * colWidth), y);
            y += 6;

            // Draw Rows (first 30 lines)
            int rowCount = Math.Min(30, dt.Rows.Count);
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    string val = dt.Rows[r][c]?.ToString() ?? "";
                    if (val.Length > 20) val = val.Substring(0, 18) + "..";
                    g.DrawString(val, bodyFont, Brushes.Black, x + (c * colWidth), y);
                }
                y += 20;
            }

            y += 10;
            g.DrawLine(Pens.Black, 40, y, 40 + (dt.Columns.Count * colWidth), y);
            y += 10;
            g.DrawString(lblTotalRevenue.Text, headerFont, Brushes.Black, 40, y);
        }
    }
}
