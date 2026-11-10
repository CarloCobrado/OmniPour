using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;

namespace OmniPour.WinForms.UI.Forms
{
    public class ReceiptPrintPreviewForm : Form
    {
        private readonly Sale _sale;
        private TextBox txtReceipt = null!;
        private ModernButton btnPrint = null!;
        private ModernButton btnClose = null!;
        private PrintDocument printDoc = null!;

        public ReceiptPrintPreviewForm(Sale sale)
        {
            _sale = sale ?? throw new ArgumentNullException(nameof(sale));
            InitializeComponent();
            GenerateReceiptContent();
        }

        private void InitializeComponent()
        {
            Text = $"Receipt Slip - {_sale.InvoiceNumber}";
            Size = new Size(460, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ModernTheme.ObsidianDark,
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblTitle = new Label
            {
                Text = "Official Customer Receipt",
                Font = ModernTheme.FontSubTitle,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 12)
            };

            var lblSub = new Label
            {
                Text = $"Invoice #{_sale.InvoiceNumber} • Verified",
                Font = ModernTheme.FontSmall,
                ForeColor = ModernTheme.AmberAccent,
                AutoSize = true,
                Location = new Point(16, 32)
            };

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblSub);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10)
            };

            btnPrint = new ModernButton
            {
                Text = "🖨 Print Slip",
                NormalColor = ModernTheme.AmberAccentDark,
                HoverColor = ModernTheme.AmberAccent,
                Width = 130,
                Height = 40,
                Location = new Point(180, 10)
            };
            btnPrint.Click += BtnPrint_Click;

            btnClose = new ModernButton
            {
                Text = "Close",
                NormalColor = Color.FromArgb(100, 116, 139),
                HoverColor = Color.FromArgb(71, 85, 105),
                Width = 100,
                Height = 40,
                Location = new Point(320, 10)
            };
            btnClose.Click += (s, e) => Close();

            bottomPanel.Controls.Add(btnPrint);
            bottomPanel.Controls.Add(btnClose);

            var centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            txtReceipt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(254, 254, 254),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Consolas", 10F),
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };

            centerPanel.Controls.Add(txtReceipt);

            Controls.Add(centerPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            printDoc = new PrintDocument();
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private void GenerateReceiptContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("         OMNIPOUR BEVERAGE SYSTEM         ");
            sb.AppendLine("   Beverage & Recipe Management Suite     ");
            sb.AppendLine("     Point-of-Sale & Inventory Station    ");
            sb.AppendLine("          System: OMNIPOUR-TPS            ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Invoice No: {_sale.InvoiceNumber}");
            sb.AppendLine($"Date & Time: {_sale.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cashier:     {_sale.CashierName}");
            sb.AppendLine($"Customer:    {_sale.CustomerName}");
            sb.AppendLine($"Method:      {_sale.PaymentMethod}");
            sb.AppendLine($"Status:      {_sale.PaymentStatus.ToUpper()}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("QTY  ITEM                     TOTAL (PHP)");
            sb.AppendLine("------------------------------------------");

            foreach (var item in _sale.Items)
            {
                string name = item.ProductName.Length > 20 ? item.ProductName.Substring(0, 20) : item.ProductName.PadRight(20);
                sb.AppendLine($"{item.Quantity,-3}  {name} {item.Subtotal,12:N2}");
            }

            sb.AppendLine("------------------------------------------");
            sb.AppendLine($"Subtotal:                    ₱{_sale.Subtotal,12:N2}");
            if (_sale.TaxAmount > 0)
                sb.AppendLine($"Tax:                         ₱{_sale.TaxAmount,12:N2}");
            if (_sale.DiscountAmount > 0)
                sb.AppendLine($"Discount:                   -₱{_sale.DiscountAmount,12:N2}");
            sb.AppendLine($"GRAND TOTAL:                 ₱{_sale.GrandTotal,12:N2}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine($"Amount Tendered:             ₱{_sale.AmountTendered,12:N2}");
            sb.AppendLine($"Change Due:                  ₱{_sale.ChangeDue,12:N2}");
            sb.AppendLine("==========================================");
            sb.AppendLine("     Thank you for using OmniPour!        ");
            sb.AppendLine("  Serving Beverage Excellence Every Day   ");
            sb.AppendLine("==========================================");

            txtReceipt.Text = sb.ToString();
            txtReceipt.SelectionStart = 0;
            txtReceipt.SelectionLength = 0;
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                using var printDlg = new PrintDialog { Document = printDoc };
                if (printDlg.ShowDialog(this) == DialogResult.OK)
                {
                    printDoc.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Printing error: {ex.Message}", "Print Slip", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            using var printFont = new Font("Consolas", 9.5F);
            e.Graphics?.DrawString(txtReceipt.Text, printFont, Brushes.Black, 20, 20);
        }
    }
}
