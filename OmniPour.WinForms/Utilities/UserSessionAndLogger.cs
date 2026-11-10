using System;
using System.IO;
using OmniPour.WinForms.Models;
using Microsoft.Data.SqlClient;

namespace OmniPour.WinForms.Utilities
{
    public static class UserSession
    {
        public static User? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Login(User user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool HasRole(string roleName)
        {
            if (CurrentUser == null) return false;
            return string.Equals(CurrentUser.RoleName, roleName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAdmin => HasRole("Administrator");
        public static bool IsSupervisor => HasRole("Supervisor") || IsAdmin;
        public static bool IsCashier => HasRole("Cashier") || HasRole("Supervisor") || IsAdmin;
        public static bool IsInventoryStaff => HasRole("Inventory Staff") || HasRole("Supervisor") || IsAdmin;

        // Specific, strict role-based permission capabilities
        public static bool CanAccessPos => HasRole("Cashier") || HasRole("Supervisor") || HasRole("Administrator");
        public static bool CanAccessInventory => HasRole("Inventory Staff") || HasRole("Supervisor") || HasRole("Administrator");
        public static bool CanAccessProducts => HasRole("Inventory Staff") || HasRole("Supervisor") || HasRole("Administrator");
        public static bool CanAccessCategories => HasRole("Inventory Staff") || HasRole("Supervisor") || HasRole("Administrator");
        public static bool CanAccessReports => HasRole("Supervisor") || HasRole("Administrator");
        public static bool CanAccessUsers => HasRole("Administrator");
        public static bool CanAccessAudit => HasRole("Supervisor") || HasRole("Administrator");
        public static bool CanAccessMaintenance => HasRole("Administrator");
        public static bool CanVoidSales => HasRole("Supervisor") || HasRole("Administrator");
    }

    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "error.log");

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch
            {
                // Fallback handled safely
            }
        }

        public static void LogException(Exception ex, string moduleName, string? connectionString = null)
        {
            try
            {
                // 1. Log to local text file
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Module: {moduleName} | User: {UserSession.CurrentUser?.Username ?? "SYSTEM"}" + Environment.NewLine +
                                  $"Message: {ex.Message}" + Environment.NewLine +
                                  $"StackTrace: {ex.StackTrace}" + Environment.NewLine +
                                  new string('-', 80) + Environment.NewLine;

                File.AppendAllText(LogFilePath, logEntry);

                // 2. Also log to SQL ErrorLogs table if connection string is provided
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using var conn = new SqlConnection(connectionString);
                    using var cmd = new SqlCommand("INSERT INTO dbo.ErrorLogs (UserId, ErrorMessage, StackTrace, SourceModule) VALUES (@UserId, @Msg, @Stack, @Mod)", conn);
                    cmd.Parameters.AddWithValue("@UserId", (object?)UserSession.CurrentUser?.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Msg", ex.Message ?? "Unknown error");
                    cmd.Parameters.AddWithValue("@Stack", (object?)ex.StackTrace ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Mod", moduleName);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ensure logger itself never crashes the application
            }
        }
    }
}
