using System;
using System.Configuration;
using OmniPour.WinForms.DataAccess;
using OmniPour.WinForms.Models;
using OmniPour.WinForms.Utilities;

namespace OmniPour.WinForms.BusinessLogic
{
    public static class AuthBLL
    {
        private static int MaxFailedAttempts
        {
            get
            {
                if (int.TryParse(ConfigurationManager.AppSettings["MaxFailedLogins"], out int val))
                    return val;
                return 3;
            }
        }

        private static int LockoutMinutes
        {
            get
            {
                if (int.TryParse(ConfigurationManager.AppSettings["LockoutMinutes"], out int val))
                    return val;
                return 5;
            }
        }

        public static (bool success, string message, User? user) Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Please enter both username and password.", null);
            }

            var user = UserDAL.GetByUsername(username.Trim());
            if (user == null)
            {
                AuditDAL.Log(null, username, "FAILED_LOGIN", "Users", null, "Failed login attempt: Username not found.");
                return (false, "Invalid username or password.", null);
            }

            if (!user.IsActive)
            {
                AuditDAL.Log(user.UserId, user.Username, "FAILED_LOGIN", "Users", user.UserId.ToString(), "Login blocked: Account is deactivated.");
                return (false, "This account has been deactivated. Please contact your system administrator.", null);
            }

            if (user.IsLockedOut)
            {
                var remaining = user.LockoutUntil!.Value - DateTime.UtcNow;
                string waitText = remaining.TotalMinutes >= 1
                    ? $"{Math.Ceiling(remaining.TotalMinutes)} minute(s)"
                    : $"{Math.Ceiling(remaining.TotalSeconds)} second(s)";

                AuditDAL.Log(user.UserId, user.Username, "FAILED_LOGIN", "Users", user.UserId.ToString(), "Login attempt while account is locked out.");
                return (false, $"Account is temporarily locked due to consecutive failed logins. Please try again in {waitText}.", null);
            }

            bool isPasswordCorrect = PasswordSecurity.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);

            if (!isPasswordCorrect)
            {
                int newFailures = user.FailedLoginAttempts + 1;
                DateTime? lockoutTime = null;

                if (newFailures >= MaxFailedAttempts)
                {
                    lockoutTime = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    UserDAL.SetLockout(user.UserId, newFailures, lockoutTime);
                    AuditDAL.Log(user.UserId, user.Username, "ACCOUNT_LOCKOUT", "Users", user.UserId.ToString(),
                        $"Account locked out after {newFailures} consecutive failed login attempts.");
                    return (false, $"Account has been locked out for {LockoutMinutes} minutes due to multiple failed attempts.", null);
                }
                else
                {
                    UserDAL.SetLockout(user.UserId, newFailures, null);
                    AuditDAL.Log(user.UserId, user.Username, "FAILED_LOGIN", "Users", user.UserId.ToString(),
                        $"Failed login attempt ({newFailures} of {MaxFailedAttempts}).");
                    return (false, $"Invalid username or password. ({MaxFailedAttempts - newFailures} attempt(s) remaining before lockout).", null);
                }
            }

            // Successful login: reset failed attempts
            UserDAL.ResetFailedAttempts(user.UserId);
            UserSession.Login(user);

            AuditDAL.Log(user.UserId, user.Username, "LOGIN", "Users", user.UserId.ToString(), $"User '{user.FullName}' ({user.RoleName}) logged in successfully.");

            return (true, "Login successful.", user);
        }

        public static void Logout()
        {
            if (UserSession.CurrentUser != null)
            {
                AuditDAL.Log(UserSession.CurrentUser.UserId, UserSession.CurrentUser.Username, "LOGOUT", "Users",
                    UserSession.CurrentUser.UserId.ToString(), "User logged out.");
                UserSession.Logout();
            }
        }
    }

    public static class ProductBLL
    {
        public static (bool isValid, string errorMessage) ValidateProduct(Product p)
        {
            if (string.IsNullOrWhiteSpace(p.SKU))
                return (false, "Product SKU / Code is required.");

            if (string.IsNullOrWhiteSpace(p.ProductName))
                return (false, "Product Name is required.");

            if (p.CategoryId <= 0)
                return (false, "Please select a valid Category.");

            if (p.UnitPrice <= 0)
                return (false, "Unit price must be greater than zero.");

            if (p.StockQuantity < 0)
                return (false, "Stock quantity cannot be negative.");

            if (p.ReorderLevel < 0)
                return (false, "Reorder level cannot be negative.");

            return (true, string.Empty);
        }

        public static (bool isValid, string errorMessage) ValidateIngredient(Ingredient ing)
        {
            if (string.IsNullOrWhiteSpace(ing.IngredientName))
                return (false, "Ingredient name is required.");

            if (string.IsNullOrWhiteSpace(ing.Unit))
                return (false, "Measurement unit is required (e.g. g, ml, pcs).");

            if (ing.StockQuantity < 0)
                return (false, "Stock quantity cannot be negative.");

            if (ing.ReorderLevel < 0)
                return (false, "Reorder level cannot be negative.");

            if (ing.CostPerUnit < 0)
                return (false, "Cost per unit cannot be negative.");

            return (true, string.Empty);
        }
    }
}
