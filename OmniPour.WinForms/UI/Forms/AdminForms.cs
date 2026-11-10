using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OmniPour.WinForms.BusinessLogic;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.UI.Theme;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.UI.Forms
{
    // ========================================================================
    // 1. USER MANAGEMENT FORM (ADMIN ONLY)
    // ========================================================================
    public class UserManagementForm : Form
    {
        private DataGridView dgvUsers = null!;
        private CheckBox chkIncludeInactive = null!;
        private ModernButton btnNewUser = null!;
        private ModernButton btnEditUser = null!;
        private ModernButton btnResetPassword = null!;
        private ModernButton btnToggleActive = null!;

        public UserManagementForm()
        {
            if (!UserSession.CanAccessUsers)
            {
                MessageBox.Show("Access Denied: Only users with the Administrator role can access User Accounts.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadUsers();
        }

        private void InitializeComponent()
        {
            Text = "User Management & Role-Based Access Control (Admin)";
            Size = new Size(1000, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "USER ACCOUNTS & SECURITY PERMISSIONS", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            var toolBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };

            chkIncludeInactive = new CheckBox { Text = "Include Deactivated Accounts", Location = new Point(20, 20), AutoSize = true, Checked = true };
            chkIncludeInactive.CheckedChanged += (s, e) => LoadUsers();

            btnNewUser = new ModernButton { Text = "+ New User", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 110, Height = 34, Location = new Point(240, 13), Font = ModernTheme.FontSmallBold };
            btnNewUser.Click += BtnNewUser_Click;

            btnEditUser = new ModernButton { Text = "Edit Profile", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 34, Location = new Point(360, 13), Font = ModernTheme.FontSmallBold };
            btnEditUser.Click += BtnEditUser_Click;

            btnResetPassword = new ModernButton { Text = "Reset Password", NormalColor = ModernTheme.IndigoInfo, HoverColor = Color.FromArgb(79, 70, 229), Width = 130, Height = 34, Location = new Point(470, 13), Font = ModernTheme.FontSmallBold };
            btnResetPassword.Click += BtnResetPassword_Click;

            btnToggleActive = new ModernButton { Text = "Toggle Active", NormalColor = ModernTheme.RoseDanger, HoverColor = Color.FromArgb(220, 38, 38), Width = 110, Height = 34, Location = new Point(610, 13), Font = ModernTheme.FontSmallBold };
            btnToggleActive.Click += BtnToggleActive_Click;

            toolBar.Controls.AddRange(new Control[] { chkIncludeInactive, btnNewUser, btnEditUser, btnResetPassword, btnToggleActive });

            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            dgvUsers = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvUsers);
            SetupColumns();
            centerPanel.Controls.Add(dgvUsers);

            Controls.Add(centerPanel);
            Controls.Add(toolBar);
            Controls.Add(topBar);
        }

        private void SetupColumns()
        {
            dgvUsers.Columns.Clear();
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "UserId", Width = 50 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Username", DataPropertyName = "Username", Width = 130 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Full Name", DataPropertyName = "FullName", Width = 180 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Email", DataPropertyName = "Email", Width = 200 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Role", DataPropertyName = "RoleName", Width = 130 });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Failed Logins", DataPropertyName = "FailedLoginAttempts", Width = 100 });
            dgvUsers.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", DataPropertyName = "IsActive", Width = 70 });
        }

        private void LoadUsers()
        {
            try
            {
                var users = UserDAL.GetAll(chkIncludeInactive.Checked);
                dgvUsers.DataSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private User? GetSelectedUser()
        {
            if (dgvUsers.CurrentRow?.DataBoundItem is User u) return u;
            return null;
        }

        private void BtnNewUser_Click(object? sender, EventArgs e)
        {
            using var dlg = new UserEditDialog(null);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadUsers();
        }

        private void BtnEditUser_Click(object? sender, EventArgs e)
        {
            var u = GetSelectedUser();
            if (u == null)
            {
                MessageBox.Show("Please select a user to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new UserEditDialog(u);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadUsers();
        }

        private void BtnResetPassword_Click(object? sender, EventArgs e)
        {
            var u = GetSelectedUser();
            if (u == null)
            {
                MessageBox.Show("Please select a user to reset password.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newPass = PromptDialog.Show("Reset User Password", $"Enter new password for '{u.Username}':", "Password123!");
            if (string.IsNullOrWhiteSpace(newPass)) return;

            var (hash, salt) = PasswordSecurity.HashPassword(newPass);
            UserDAL.ResetPassword(u.UserId, hash, salt);

            AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "ADMIN", "PASSWORD_RESET", "Users", u.UserId.ToString(),
                $"Admin reset password for user '{u.Username}'.");

            MessageBox.Show($"Password for '{u.Username}' has been reset successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
        }

        private void BtnToggleActive_Click(object? sender, EventArgs e)
        {
            var u = GetSelectedUser();
            if (u == null)
            {
                MessageBox.Show("Please select a user to toggle status.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (u.UserId == UserSession.CurrentUser?.UserId)
            {
                MessageBox.Show("You cannot deactivate your own currently active account.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string action = u.IsActive ? "deactivate (soft-delete)" : "reactivate";
            if (MessageBox.Show($"Are you sure you want to {action} account '{u.Username}'?", "Confirm Action", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            u.IsActive = !u.IsActive;
            UserDAL.UpdateUser(u);

            AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "ADMIN", "USER_DEACTIVATE", "Users", u.UserId.ToString(),
                $"Toggled user '{u.Username}' active status to {u.IsActive}.");

            LoadUsers();
        }
    }

    public class UserEditDialog : Form
    {
        private readonly User? _user;
        private TextBox txtUsername = null!;
        private TextBox txtFullName = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPassword = null!;
        private ComboBox cboRole = null!;
        private CheckBox chkActive = null!;
        private ModernButton btnSave = null!;

        public UserEditDialog(User? user)
        {
            _user = user;
            InitializeComponent();
            PopulateData();
        }

        private void InitializeComponent()
        {
            Text = _user == null ? "Create New User" : $"Edit User - {_user.Username}";
            Size = new Size(460, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = ModernTheme.FontBody;

            int left = 30, fieldLeft = 140, y = 30, gap = 44;

            var lblUser = new Label { Text = "Username:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtUsername = new TextBox { Location = new Point(fieldLeft, y), Width = 260 };

            y += gap;
            var lblName = new Label { Text = "Full Name:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtFullName = new TextBox { Location = new Point(fieldLeft, y), Width = 260 };

            y += gap;
            var lblEmail = new Label { Text = "Email Address:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtEmail = new TextBox { Location = new Point(fieldLeft, y), Width = 260 };

            y += gap;
            var lblPass = new Label { Text = "Initial Pass:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            txtPassword = new TextBox { Location = new Point(fieldLeft, y), Width = 260, Text = "Password123!" };
            if (_user != null)
            {
                txtPassword.Enabled = false;
                txtPassword.Text = "(Use Reset Password)";
            }

            y += gap;
            var lblRole = new Label { Text = "Role:", Location = new Point(left, y + 4), AutoSize = true, Font = ModernTheme.FontSubTitle };
            cboRole = new ComboBox { Location = new Point(fieldLeft, y), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            y += gap;
            chkActive = new CheckBox { Text = "Account is Active & Allowed to Sign In", Location = new Point(fieldLeft, y), AutoSize = true, Checked = true, Font = ModernTheme.FontBodyBold };

            y += 50;
            btnSave = new ModernButton { Text = "Save Account", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 140, Height = 38, Location = new Point(140, y) };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new ModernButton { Text = "Cancel", NormalColor = Color.FromArgb(100, 116, 139), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 38, Location = new Point(290, y) };
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                lblUser, txtUsername, lblName, txtFullName, lblEmail, txtEmail,
                lblPass, txtPassword, lblRole, cboRole, chkActive, btnSave, btnCancel
            });
        }

        private void PopulateData()
        {
            var roles = UserDAL.GetAllRoles();
            cboRole.DataSource = roles;
            cboRole.DisplayMember = "RoleName";
            cboRole.ValueMember = "RoleId";

            if (_user != null)
            {
                txtUsername.Text = _user.Username;
                txtUsername.ReadOnly = true;
                txtFullName.Text = _user.FullName;
                txtEmail.Text = _user.Email;
                cboRole.SelectedValue = _user.RoleId;
                chkActive.Checked = _user.IsActive;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Please fill out all required account fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_user == null)
                {
                    var (hash, salt) = PasswordSecurity.HashPassword(txtPassword.Text);
                    var newUser = new User
                    {
                        Username = txtUsername.Text.Trim(),
                        FullName = txtFullName.Text.Trim(),
                        Email = txtEmail.Text.Trim(),
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        RoleId = (int)cboRole.SelectedValue!,
                        IsActive = chkActive.Checked
                    };

                    UserDAL.CreateUser(newUser);
                    AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "ADMIN", "USER_CREATE", "Users", newUser.Username, $"Created new user '{newUser.FullName}'.");
                }
                else
                {
                    _user.FullName = txtFullName.Text.Trim();
                    _user.Email = txtEmail.Text.Trim();
                    _user.RoleId = (int)cboRole.SelectedValue!;
                    _user.IsActive = chkActive.Checked;

                    UserDAL.UpdateUser(_user);
                    AuditDAL.Log(UserSession.CurrentUser?.UserId, UserSession.CurrentUser?.Username ?? "ADMIN", "USER_UPDATE", "Users", _user.UserId.ToString(), $"Updated user '{_user.FullName}'.");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save user account: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ========================================================================
    // 2. AUDIT TRAIL FORM (ADMIN / SUPERVISOR ONLY)
    // ========================================================================
    public class AuditTrailForm : Form
    {
        private DataGridView dgvAudit = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboAction = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;

        public AuditTrailForm()
        {
            if (!UserSession.CanAccessAudit)
            {
                MessageBox.Show("Access Denied: Only Supervisors or Administrators can view the Audit Trail.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            LoadAuditLogs();
        }

        private void InitializeComponent()
        {
            Text = "System Audit Trail & Security Event Explorer (Admin)";
            Size = new Size(1150, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "IMMUTABLE AUDIT TRAIL & SYSTEM ACTIVITY LOG", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            var filterBar = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };

            var lblSearch = new Label { Text = "Keyword:", Location = new Point(20, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            txtSearch = new TextBox { Location = new Point(85, 16), Width = 180, PlaceholderText = "User, Table, or Detail..." };
            txtSearch.TextChanged += (s, e) => LoadAuditLogs();

            var lblAct = new Label { Text = "Action:", Location = new Point(285, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            cboAction = new ComboBox { Location = new Point(335, 16), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cboAction.Items.AddRange(new object[] { "All Actions", "LOGIN", "LOGOUT", "FAILED_LOGIN", "SALE_COMPLETE", "SALE_VOID", "INVENTORY_TX", "BACKUP", "RESTORE" });
            cboAction.SelectedIndex = 0;
            cboAction.SelectedIndexChanged += (s, e) => LoadAuditLogs();

            var lblFrom = new Label { Text = "From:", Location = new Point(495, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            dtpFrom = new DateTimePicker { Location = new Point(535, 16), Width = 115, Value = DateTime.Today.AddDays(-7) };
            dtpFrom.ValueChanged += (s, e) => LoadAuditLogs();

            var lblTo = new Label { Text = "To:", Location = new Point(660, 20), AutoSize = true, Font = ModernTheme.FontSmallBold };
            dtpTo = new DateTimePicker { Location = new Point(685, 16), Width = 115, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => LoadAuditLogs();

            var btnRefresh = new ModernButton { Text = "🔄 Refresh", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 100, Height = 34, Location = new Point(820, 14), Font = ModernTheme.FontSmallBold };
            btnRefresh.Click += (s, e) => LoadAuditLogs();

            filterBar.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblAct, cboAction, lblFrom, dtpFrom, lblTo, dtpTo, btnRefresh });

            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            dgvAudit = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true };
            ModernTheme.StyleDataGridView(dgvAudit);
            SetupColumns();
            centerPanel.Controls.Add(dgvAudit);

            Controls.Add(centerPanel);
            Controls.Add(filterBar);
            Controls.Add(topBar);
        }

        private void SetupColumns()
        {
            dgvAudit.Columns.Clear();
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "AuditId", Width = 60 });
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Timestamp", DataPropertyName = "Timestamp", Width = 150, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" } });
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "User", DataPropertyName = "Username", Width = 110 });
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = "Action", Width = 130 });
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Entity / Table", DataPropertyName = "TableName", Width = 110 });
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Description & Details", DataPropertyName = "Description", Width = 380 });
            dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Host / Machine", DataPropertyName = "IpAddress", Width = 120 });
        }

        private void LoadAuditLogs()
        {
            try
            {
                var logs = AuditDAL.GetLogs(txtSearch.Text, cboAction.SelectedItem?.ToString(), dtpFrom.Value, dtpTo.Value);
                dgvAudit.DataSource = logs;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading audit logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ========================================================================
    // 3. DATABASE BACKUP & RESTORE FORM (ADMIN ONLY)
    // ========================================================================
    public class DatabaseMaintenanceForm : Form
    {
        private TextBox txtBackupPath = null!;
        private TextBox txtRestorePath = null!;
        private ModernButton btnBrowseBackup = null!;
        private ModernButton btnExecuteBackup = null!;
        private ModernButton btnBrowseRestore = null!;
        private ModernButton btnExecuteRestore = null!;

        public DatabaseMaintenanceForm()
        {
            if (!UserSession.CanAccessMaintenance)
            {
                MessageBox.Show("Access Denied: Only users with the Administrator role can access Database Maintenance.",
                    "Security Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Database Maintenance, Backup & Disaster Recovery (Admin)";
            Size = new Size(800, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ModernTheme.CanvasBackground;
            Font = ModernTheme.FontBody;

            var topBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ModernTheme.ObsidianDark, Padding = new Padding(20, 12, 20, 12) };
            var lblTitle = new Label { Text = "DATABASE BACKUP & DISASTER RESTORATION", Font = ModernTheme.FontSubTitle, ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18) };
            topBar.Controls.Add(lblTitle);

            // Card 1: Backup
            var cardBackup = new ModernCard { Location = new Point(20, 80), Size = new Size(740, 170), SurfaceColor = Color.White };
            var lblBkTitle = new Label { Text = "Create Database Backup (.BAK)", Font = ModernTheme.FontSubTitle, ForeColor = ModernTheme.TextPrimary, Location = new Point(16, 16), AutoSize = true };
            var lblBkDesc = new Label { Text = "Creates an atomic, consistent point-in-time full backup of the 'OmniPourDB' database.", Font = ModernTheme.FontSmall, ForeColor = ModernTheme.TextSecondary, Location = new Point(16, 40), AutoSize = true };

            txtBackupPath = new TextBox
            {
                Location = new Point(16, 70),
                Width = 550,
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"OmniPourDB_Backup_{DateTime.Now:yyyyMMdd_HHmm}.bak")
            };

            btnBrowseBackup = new ModernButton { Text = "Browse...", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 30, Location = new Point(580, 66), Font = ModernTheme.FontSmallBold };
            btnBrowseBackup.Click += BtnBrowseBackup_Click;

            btnExecuteBackup = new ModernButton { Text = "🛡 Create Full Backup Now", NormalColor = ModernTheme.AmberAccentDark, HoverColor = ModernTheme.AmberAccent, Width = 220, Height = 38, Location = new Point(16, 112) };
            btnExecuteBackup.Click += BtnExecuteBackup_Click;

            cardBackup.Controls.AddRange(new Control[] { lblBkTitle, lblBkDesc, txtBackupPath, btnBrowseBackup, btnExecuteBackup });

            // Card 2: Restore
            var cardRestore = new ModernCard { Location = new Point(20, 270), Size = new Size(740, 180), SurfaceColor = Color.White };
            var lblResTitle = new Label { Text = "Restore Database from Backup File", Font = ModernTheme.FontSubTitle, ForeColor = ModernTheme.RoseDanger, Location = new Point(16, 16), AutoSize = true };
            var lblResDesc = new Label { Text = "WARNING: Restoring will overwrite existing tables with the backup archive data. Ensure all other sessions are closed.", Font = ModernTheme.FontSmallBold, ForeColor = ModernTheme.RoseDanger, Location = new Point(16, 40), AutoSize = true };

            txtRestorePath = new TextBox { Location = new Point(16, 75), Width = 550, PlaceholderText = "Select a valid .bak file to restore..." };

            btnBrowseRestore = new ModernButton { Text = "Browse...", NormalColor = Color.FromArgb(51, 65, 85), HoverColor = Color.FromArgb(71, 85, 105), Width = 100, Height = 30, Location = new Point(580, 71), Font = ModernTheme.FontSmallBold };
            btnBrowseRestore.Click += BtnBrowseRestore_Click;

            btnExecuteRestore = new ModernButton { Text = "⚠️ Restore Database", NormalColor = ModernTheme.RoseDanger, HoverColor = Color.FromArgb(220, 38, 38), Width = 180, Height = 38, Location = new Point(16, 120) };
            btnExecuteRestore.Click += BtnExecuteRestore_Click;

            cardRestore.Controls.AddRange(new Control[] { lblResTitle, lblResDesc, txtRestorePath, btnBrowseRestore, btnExecuteRestore });

            Controls.AddRange(new Control[] { topBar, cardBackup, cardRestore });
        }

        private void BtnBrowseBackup_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "SQL Server Backup (*.bak)|*.bak",
                FileName = $"OmniPourDB_Backup_{DateTime.Now:yyyyMMdd_HHmm}.bak"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                txtBackupPath.Text = sfd.FileName;
            }
        }

        private void BtnExecuteBackup_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBackupPath.Text))
            {
                MessageBox.Show("Please specify a valid backup destination path.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor = Cursors.WaitCursor;
            btnExecuteBackup.Enabled = false;

            try
            {
                var result = DatabaseMaintenanceDAL.BackupDatabase(txtBackupPath.Text.Trim());
                if (result.success)
                {
                    MessageBox.Show(result.message + $"\n\nFile location: {txtBackupPath.Text}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(result.message, "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                btnExecuteBackup.Enabled = true;
            }
        }

        private void BtnBrowseRestore_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "SQL Server Backup (*.bak)|*.bak"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtRestorePath.Text = ofd.FileName;
            }
        }

        private void BtnExecuteRestore_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRestorePath.Text) || !File.Exists(txtRestorePath.Text))
            {
                MessageBox.Show("Please choose an existing, valid .bak backup file.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "CRITICAL WARNING:\n\nRestoring the database will overwrite current database records with the chosen backup file.\n\nAre you sure you want to proceed?",
                "Confirm Disaster Restoration", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            btnExecuteRestore.Enabled = false;

            try
            {
                var result = DatabaseMaintenanceDAL.RestoreDatabase(txtRestorePath.Text.Trim());
                if (result.success)
                {
                    MessageBox.Show(result.message, "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(result.message, "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                btnExecuteRestore.Enabled = true;
            }
        }
    }
}
