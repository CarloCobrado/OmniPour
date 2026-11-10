using System;
using System.Collections.Generic;
using System.Data;
using OmniPour.WinForms.Models;
using Microsoft.Data.SqlClient;

namespace OmniPour.WinForms.DataAccess
{
    public static class AuditDAL
    {
        public static void Log(int? userId, string username, string action, string? tableName = null, string? recordId = null, string description = "")
        {
            const string sql = @"
                INSERT INTO dbo.AuditTrail (UserId, Username, Action, TableName, RecordId, Description, IpAddress)
                VALUES (@UserId, @Username, @Action, @TableName, @RecordId, @Description, @IpAddress)";

            var parameters = new[]
            {
                new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
                new SqlParameter("@Username", username ?? "SYSTEM"),
                new SqlParameter("@Action", action),
                new SqlParameter("@TableName", (object?)tableName ?? DBNull.Value),
                new SqlParameter("@RecordId", (object?)recordId ?? DBNull.Value),
                new SqlParameter("@Description", description),
                new SqlParameter("@IpAddress", Environment.MachineName)
            };

            try
            {
                DatabaseHelper.ExecuteNonQuery(sql, parameters);
            }
            catch
            {
                // Never crash the primary workflow if an audit log fails
            }
        }

        public static List<AuditLog> GetLogs(string? searchKeyword = null, string? actionFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = new List<AuditLog>();
            string sql = @"
                SELECT AuditId, UserId, Username, Action, TableName, RecordId, Description, IpAddress, Timestamp
                FROM dbo.AuditTrail
                WHERE (1 = 1) ";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                sql += " AND (Username LIKE @Keyword OR Description LIKE @Keyword OR TableName LIKE @Keyword) ";
                parameters.Add(new SqlParameter("@Keyword", $"%{searchKeyword.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(actionFilter) && actionFilter != "All Actions")
            {
                sql += " AND Action = @Action ";
                parameters.Add(new SqlParameter("@Action", actionFilter.Trim()));
            }

            if (fromDate.HasValue)
            {
                sql += " AND Timestamp >= @FromDate ";
                parameters.Add(new SqlParameter("@FromDate", fromDate.Value.Date));
            }

            if (toDate.HasValue)
            {
                sql += " AND Timestamp < @ToDate ";
                parameters.Add(new SqlParameter("@ToDate", toDate.Value.Date.AddDays(1)));
            }

            sql += " ORDER BY Timestamp DESC";

            var dt = DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AuditLog
                {
                    AuditId = Convert.ToInt32(row["AuditId"]),
                    UserId = row["UserId"] != DBNull.Value ? Convert.ToInt32(row["UserId"]) : null,
                    Username = row["Username"].ToString() ?? "",
                    Action = row["Action"].ToString() ?? "",
                    TableName = row["TableName"]?.ToString() ?? "",
                    RecordId = row["RecordId"]?.ToString() ?? "",
                    Description = row["Description"].ToString() ?? "",
                    IpAddress = row["IpAddress"]?.ToString() ?? "",
                    Timestamp = Convert.ToDateTime(row["Timestamp"])
                });
            }

            return list;
        }
    }

    public static class UserDAL
    {
        public static User? GetByUsername(string username)
        {
            const string sql = @"
                SELECT u.UserId, u.Username, u.FullName, u.Email, u.PasswordHash, u.PasswordSalt,
                       u.RoleId, r.RoleName, u.FailedLoginAttempts, u.LockoutUntil, u.IsActive,
                       u.CreatedAt, u.UpdatedAt, u.RowVersion
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                WHERE u.Username = @Username";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Username", username.Trim()) });
            if (dt.Rows.Count == 0) return null;

            return MapUser(dt.Rows[0]);
        }

        public static List<User> GetAll(bool includeInactive = true)
        {
            var list = new List<User>();
            string sql = @"
                SELECT u.UserId, u.Username, u.FullName, u.Email, u.PasswordHash, u.PasswordSalt,
                       u.RoleId, r.RoleName, u.FailedLoginAttempts, u.LockoutUntil, u.IsActive,
                       u.CreatedAt, u.UpdatedAt, u.RowVersion
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId ";

            if (!includeInactive)
                sql += " WHERE u.IsActive = 1 ";

            sql += " ORDER BY u.FullName ASC";

            var dt = DatabaseHelper.ExecuteQuery(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapUser(row));
            }
            return list;
        }

        public static List<Role> GetAllRoles()
        {
            var list = new List<Role>();
            const string sql = "SELECT RoleId, RoleName, Description FROM dbo.Roles ORDER BY RoleId ASC";
            var dt = DatabaseHelper.ExecuteQuery(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Role
                {
                    RoleId = Convert.ToInt32(row["RoleId"]),
                    RoleName = row["RoleName"].ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public static int CreateUser(User user)
        {
            const string sql = @"
                INSERT INTO dbo.Users (Username, FullName, Email, PasswordHash, PasswordSalt, RoleId, IsActive)
                OUTPUT INSERTED.UserId
                VALUES (@Username, @FullName, @Email, @PasswordHash, @PasswordSalt, @RoleId, @IsActive)";

            var parameters = new[]
            {
                new SqlParameter("@Username", user.Username.Trim()),
                new SqlParameter("@FullName", user.FullName.Trim()),
                new SqlParameter("@Email", user.Email.Trim()),
                new SqlParameter("@PasswordHash", user.PasswordHash),
                new SqlParameter("@PasswordSalt", user.PasswordSalt),
                new SqlParameter("@RoleId", user.RoleId),
                new SqlParameter("@IsActive", user.IsActive)
            };

            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, parameters));
        }

        public static void UpdateUser(User user)
        {
            const string sql = @"
                UPDATE dbo.Users
                SET FullName = @FullName,
                    Email = @Email,
                    RoleId = @RoleId,
                    IsActive = @IsActive,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@FullName", user.FullName.Trim()),
                new SqlParameter("@Email", user.Email.Trim()),
                new SqlParameter("@RoleId", user.RoleId),
                new SqlParameter("@IsActive", user.IsActive),
                new SqlParameter("@UserId", user.UserId)
            };

            DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public static void ResetPassword(int userId, string newHash, string newSalt)
        {
            const string sql = @"
                UPDATE dbo.Users
                SET PasswordHash = @Hash,
                    PasswordSalt = @Salt,
                    FailedLoginAttempts = 0,
                    LockoutUntil = NULL,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@Hash", newHash),
                new SqlParameter("@Salt", newSalt),
                new SqlParameter("@UserId", userId)
            };

            DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public static void SetLockout(int userId, int failedAttempts, DateTime? lockoutUntil)
        {
            const string sql = @"
                UPDATE dbo.Users
                SET FailedLoginAttempts = @FailedAttempts,
                    LockoutUntil = @LockoutUntil
                WHERE UserId = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@FailedAttempts", failedAttempts),
                new SqlParameter("@LockoutUntil", (object?)lockoutUntil ?? DBNull.Value),
                new SqlParameter("@UserId", userId)
            };

            DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public static void ResetFailedAttempts(int userId)
        {
            const string sql = "UPDATE dbo.Users SET FailedLoginAttempts = 0, LockoutUntil = NULL WHERE UserId = @UserId";
            DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@UserId", userId) });
        }

        private static User MapUser(DataRow row)
        {
            return new User
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Username = row["Username"].ToString() ?? "",
                FullName = row["FullName"].ToString() ?? "",
                Email = row["Email"].ToString() ?? "",
                PasswordHash = row["PasswordHash"].ToString() ?? "",
                PasswordSalt = row["PasswordSalt"].ToString() ?? "",
                RoleId = Convert.ToInt32(row["RoleId"]),
                RoleName = row["RoleName"].ToString() ?? "",
                FailedLoginAttempts = Convert.ToInt32(row["FailedLoginAttempts"]),
                LockoutUntil = row["LockoutUntil"] != DBNull.Value ? Convert.ToDateTime(row["LockoutUntil"]) : null,
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(row["UpdatedAt"]),
                RowVersion = (byte[])row["RowVersion"]
            };
        }
    }
}
