using System;
using System.Drawing;
using System.Windows.Forms;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    public class SalesHistoryForm : Form
    {
        private DataGridView dgvSales = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboStatus = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private ModernButton btnVoid = null!;
        private ModernButton btnReprint = null!;

        public SalesHistoryForm()
        {
            InitializeComponent();
            LoadSales();
        }

        private void InitializeComponent()
        {
            Text = "Sales History & Transaction Voiding";
            Size = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            // Header Bar
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ModernTheme.ObsidianDark,
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblTitle = new Label
            {
                Text = "TRANSACTION REGISTRY & AUDIT",
                Font = ModernTheme.FontSubTitle,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 18)
            };
            topBar.Controls.Add(lblTitle);

            // Filter Toolbar
            var filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblSearch = new Label { Text = "Search:", Location = new Point(20, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtSearch = new TextBox { Location = new Point(75, 16), Width = 160, PlaceholderText = "Invoice # or Customer" };
            txtSearch.TextChanged += (s, e) => LoadSales();

            var lblStatus = new Label { Text = "Status:", Location = new Point(255, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboStatus = new ComboBox { Location = new Point(305, 16), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.Items.AddRange(new object[] { "All", "Paid", "Voided" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => LoadSales();

            var lblFrom = new Label { Text = "From:", Location = new Point(435, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            dtpFrom = new DateTimePicker { Location = new Point(475, 16), Width = 115, Value = DateTime.Today.AddDays(-7) };
            dtpFrom.ValueChanged += (s, e) => LoadSales();

            var lblTo = new Label { Text = "To:", Location = new Point(605, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            dtpTo = new DateTimePicker { Location = new Point(635, 16), Width = 115, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => LoadSales();

            btnReprint = new ModernButton
            {
                Text = "Reprint Slip",
                NormalColor = ModernTheme.AmberAccentDark,
                HoverColor = ModernTheme.AmberAccent,
                Width = 110,
                Height = 32,
                Location = new Point(770, 14),
                Font = ModernTheme.FontSmallBold
            };
            btnReprint.Click += BtnReprint_Click;

            btnVoid = new ModernButton
            {
                Text = "Void Transaction",
                NormalColor = ModernTheme.RoseDanger,
                HoverColor = Color.FromArgb(220, 38, 38),
                Width = 130,
                Height = 32,
                Location = new Point(890, 14),
                Font = ModernTheme.FontSmallBold,
                Visible = UserSession.CanVoidSales
            };
            btnVoid.Click += BtnVoid_Click;

            filterBar.Controls.AddRange(new Control[] {
                lblSearch, txtSearch, lblStatus, cboStatus,
                lblFrom, dtpFrom, lblTo, dtpTo,
                btnReprint, btnVoid
            });

            // Grid Panel
            var centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            dgvSales = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            ModernTheme.StyleDataGridView(dgvSales);
            SetupGridColumns();

            centerPanel.Controls.Add(dgvSales);

            Controls.Add(centerPanel);
            Controls.Add(filterBar);
            Controls.Add(topBar);
        }

        private void SetupGridColumns()
        {
            dgvSales.Columns.Clear();
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "SaleId", Width = 50 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Invoice #", DataPropertyName = "InvoiceNumber", Width = 140 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date/Time", DataPropertyName = "CreatedAt", Width = 150, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" } });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cashier", DataPropertyName = "CashierName", Width = 130 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Customer", DataPropertyName = "CustomerName", Width = 140 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total (₱)", DataPropertyName = "GrandTotal", Width = 100, DefaultCellStyle = { Format = "N2" } });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Method", DataPropertyName = "PaymentMethod", Width = 80 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "PaymentStatus", Width = 90 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Void Reason", DataPropertyName = "VoidReason", Width = 160 });

            // Status Badge Formatting
            dgvSales.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 7 && e.Value != null && e.CellStyle != null)
                {
                    string status = e.Value.ToString() ?? "";
                    if (status == "Paid")
                    {
                        e.CellStyle.ForeColor = ModernTheme.EmeraldSuccess;
                        e.CellStyle.Font = ModernTheme.FontBodyBold;
                    }
                    else if (status == "Voided")
                    {
                        e.CellStyle.ForeColor = ModernTheme.RoseDanger;
                        e.CellStyle.Font = ModernTheme.FontBodyBold;
                    }
                }
            };
        }

        private void LoadSales()
        {
            try
            {
                var sales = SaleDAL.GetSales(dtpFrom.Value, dtpTo.Value, cboStatus.SelectedItem?.ToString(), txtSearch.Text);
                dgvSales.DataSource = sales;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sales history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Sale? GetSelectedSale()
        {
            if (dgvSales.CurrentRow?.DataBoundItem is Sale sale)
                return sale;
            return null;
        }

        private void BtnReprint_Click(object? sender, EventArgs e)
        {
            var sale = GetSelectedSale();
            if (sale == null)
            {
                MessageBox.Show("Please select a transaction to reprint.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            sale.Items = SaleDAL.GetSaleDetails(sale.SaleId);
            using var receipt = new ReceiptPrintPreviewForm(sale);
            receipt.ShowDialog(this);
        }

        private void BtnVoid_Click(object? sender, EventArgs e)
        {
            var sale = GetSelectedSale();
            if (sale == null)
            {
                MessageBox.Show("Please select a transaction to void.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (sale.PaymentStatus == "Voided")
            {
                MessageBox.Show("This transaction is already voided.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Role restriction check: Only Supervisor or Admin can void transactions
            if (!UserSession.IsSupervisor)
            {
                MessageBox.Show("Permission Denied: Only a Supervisor or Administrator can authorize voiding a transaction.",
                    "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string reason = PromptDialog.Show("Transaction Void Authorization",
                $"Enter reason for voiding transaction #{sale.InvoiceNumber}:", "Customer requested cancellation");

            if (string.IsNullOrWhiteSpace(reason))
                return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to VOID transaction #{sale.InvoiceNumber}?\n\nThis will restore all deducted stock and recipe ingredients atomically.",
                "Confirm Void", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var result = SaleDAL.VoidSale(sale.SaleId, UserSession.CurrentUser?.UserId ?? 1, reason);
            if (result.success)
            {
                MessageBox.Show(result.message, "Transaction Voided", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSales();
            }
            else
            {
                MessageBox.Show(result.message, "Void Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public static class PromptDialog
    {
        public static string Show(string caption, string prompt, string defaultVal = "")
        {
            using var form = new Form
            {
                Width = 460,
                Height = 210,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = ModernTheme.FontBody
            };

            var lbl = new Label { Left = 24, Top = 20, Text = prompt, AutoSize = true, Font = ModernTheme.FontSubTitle };
            var txt = new TextBox { Left = 24, Top = 55, Width = 390, Text = defaultVal, Font = new Font("Segoe UI", 11F) };
            var btnOk = new ModernButton { Text = "Confirm", Left = 180, Top = 105, Width = 110, Height = 36, DialogResult = DialogResult.OK };
            var btnCancel = new ModernButton { Text = "Cancel", Left = 304, Top = 105, Width = 110, Height = 36, DialogResult = DialogResult.Cancel, NormalColor = Color.FromArgb(100, 116, 139) };

            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog() == DialogResult.OK ? txt.Text : string.Empty;
        }
    }
}
