using System;
using System.Drawing;
using System.Windows.Forms;
using OmniPour.WinForms.BusinessLogic;
using OmniPour.WinForms.UI.Theme;

namespace OmniPour.WinForms.UI.Forms
{
    public class LoginForm : Form
    {
        private Panel leftBrandPanel = null!;
        private Panel rightLoginPanel = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private ModernButton btnLogin = null!;
        private Label lblError = null!;
        private ComboBox cboDemoUsers = null!;
        private CheckBox chkShowPassword = null!;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "OmniPour Suite - Secure Authentication";
            Size = new Size(880, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            // Split Layout: Left Branding (Obsidian Dark), Right Form (Card Surface)
            leftBrandPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = ModernTheme.ObsidianDark
            };
            leftBrandPanel.Paint += LeftBrandPanel_Paint;

            rightLoginPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(48)
            };

            BuildLeftBrandContent();
            BuildRightLoginContent();

            Controls.Add(rightLoginPanel);
            Controls.Add(leftBrandPanel);
        }

        private void BuildLeftBrandContent()
        {
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(36)
            };

            var lblBadge = new Label
            {
                Text = "ENTERPRISE POS & ERP",
                ForeColor = ModernTheme.AmberAccent,
                Font = ModernTheme.FontSmallBold,
                AutoSize = true,
                Location = new Point(36, 60)
            };

            var lblTitle = new Label
            {
                Text = "OmniPour",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(36, 85)
            };

            var lblSub = new Label
            {
                Text = "Beverage Transaction Processing\n& Recipe Inventory Management",
                ForeColor = ModernTheme.TextMuted,
                Font = new Font("Segoe UI", 10.5F),
                AutoSize = true,
                Location = new Point(38, 140)
            };

            var lblFeatures = new Label
            {
                Text = "✓  Atomic Multi-Table Transactions\n" +
                       "✓  Salted PBKDF2 Cryptographic Security\n" +
                       "✓  Role-Based Access Enforcement\n" +
                       "✓  Real-Time Recipe Ingredient Tracking\n" +
                       "✓  Automated Audit Trails & Concurrency",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = ModernTheme.FontBody,
                AutoSize = true,
                Location = new Point(38, 240)
            };

            var lblFooter = new Label
            {
                Text = "Final Project Edition • MSSQL Server • ADO.NET",
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = ModernTheme.FontSmall,
                AutoSize = true,
                Location = new Point(38, 460)
            };

            pnlBrand.Controls.AddRange(new Control[] { lblBadge, lblTitle, lblSub, lblFeatures, lblFooter });
            leftBrandPanel.Controls.Add(pnlBrand);
        }

        private void BuildRightLoginContent()
        {
            int startY = 50;

            var lblWelcome = new Label
            {
                Text = "Welcome Back",
                Font = ModernTheme.FontDisplay,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(48, startY)
            };

            var lblInstruction = new Label
            {
                Text = "Sign in with your role-assigned credentials to continue.",
                Font = ModernTheme.FontBody,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(48, startY + 36)
            };

            // Quick Demo Account Selector (Helpful for defense demo)
            var lblDemo = new Label
            {
                Text = "Quick Fill Account (Demo / Defense):",
                Font = ModernTheme.FontSmallBold,
                ForeColor = ModernTheme.TextMuted,
                AutoSize = true,
                Location = new Point(48, startY + 76)
            };

            cboDemoUsers = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ModernTheme.FontSmall,
                Location = new Point(48, startY + 98),
                Width = 380,
                Height = 28
            };
            cboDemoUsers.Items.AddRange(new object[] {
                "-- Select Demo Role to Populate --",
                "admin (Administrator)",
                "supervisor (Supervisor)",
                "cashier (Cashier)",
                "inventory (Inventory Staff)"
            });
            cboDemoUsers.SelectedIndex = 0;
            cboDemoUsers.SelectedIndexChanged += CboDemoUsers_SelectedIndexChanged;

            // Username Field
            var lblUser = new Label
            {
                Text = "Username",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(48, startY + 140)
            };

            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(48, startY + 165),
                Width = 380,
                Height = 32
            };

            // Password Field
            var lblPass = new Label
            {
                Text = "Password",
                Font = ModernTheme.FontSubTitle,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(48, startY + 210)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                Location = new Point(48, startY + 235),
                Width = 380,
                Height = 32,
                UseSystemPasswordChar = true
            };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformLogin(); };

            chkShowPassword = new CheckBox
            {
                Text = "Show Password",
                Font = ModernTheme.FontSmall,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(48, startY + 274)
            };
            chkShowPassword.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            // Error Label
            lblError = new Label
            {
                ForeColor = ModernTheme.RoseDanger,
                Font = ModernTheme.FontBodyBold,
                AutoSize = false,
                Size = new Size(380, 42),
                Location = new Point(48, startY + 300),
                Text = ""
            };

            // Login Button
            btnLogin = new ModernButton
            {
                Text = "Sign In to Terminal",
                NormalColor = ModernTheme.AmberAccentDark,
                HoverColor = ModernTheme.AmberAccent,
                Location = new Point(48, startY + 348),
                Width = 380,
                Height = 44,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnLogin.Click += (s, e) => PerformLogin();

            rightLoginPanel.Controls.AddRange(new Control[] {
                lblWelcome, lblInstruction, lblDemo, cboDemoUsers,
                lblUser, txtUsername, lblPass, txtPassword, chkShowPassword,
                lblError, btnLogin
            });
        }

        private void CboDemoUsers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            switch (cboDemoUsers.SelectedIndex)
            {
                case 1:
                    txtUsername.Text = "admin";
                    txtPassword.Text = "Password123!";
                    break;
                case 2:
                    txtUsername.Text = "supervisor";
                    txtPassword.Text = "Password123!";
                    break;
                case 3:
                    txtUsername.Text = "cashier";
                    txtPassword.Text = "Password123!";
                    break;
                case 4:
                    txtUsername.Text = "inventory";
                    txtPassword.Text = "Password123!";
                    break;
            }
        }

        private void PerformLogin()
        {
            lblError.Text = "";
            btnLogin.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var result = AuthBLL.Authenticate(txtUsername.Text, txtPassword.Text);
                if (!result.success)
                {
                    lblError.Text = result.message;
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    return;
                }

                // Login successful: open Dashboard
                Hide();
                using var dashboard = new MainDashboardForm();
                dashboard.ShowDialog();

                // When dashboard closes (logout)
                txtPassword.Text = "";
                lblError.Text = "";
                Show();
                txtUsername.Focus();
            }
            catch (Exception ex)
            {
                lblError.Text = "Connection error: " + ex.Message;
            }
            finally
            {
                btnLogin.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void LeftBrandPanel_Paint(object? sender, PaintEventArgs e)
        {
            // Subtle ambient gold accent line at the bottom
            using var brush = new SolidBrush(ModernTheme.AmberAccent);
            e.Graphics.FillRectangle(brush, 0, leftBrandPanel.Height - 6, leftBrandPanel.Width, 6);
        }
    }
}
