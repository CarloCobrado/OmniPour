using System;
using System.Configuration;
using System.Data;
using OmniPour.WinForms.Utilities;
using Microsoft.Data.SqlClient;

namespace OmniPour.WinForms.DataAccess
{
    public static class DatabaseHelper
    {
        private static readonly string ConnectionString;

        static DatabaseHelper()
        {
            var configSetting = ConfigurationManager.ConnectionStrings["OmniPourDB"] 
                             ?? ConfigurationManager.ConnectionStrings["OmniPourDB"];
            if (configSetting == null || string.IsNullOrWhiteSpace(configSetting.ConnectionString))
            {
                throw new InvalidOperationException("Connection string 'OmniPourDB' or 'OmniPourDB' was not found in App.config.");
            }
            ConnectionString = configSetting.ConnectionString;
        }

        public static string GetConnectionString() => ConnectionString;

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            try
            {
                using var conn = GetConnection();
                using var cmd = new SqlCommand(query, conn);
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                Logger.LogException(ex, "DatabaseHelper.ExecuteQuery", ConnectionString);
                throw new ApplicationException("A database error occurred while retrieving data. Please try again or contact your administrator.", ex);
            }
        }

        public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
        {
            try
            {
                using var conn = GetConnection();
                using var cmd = new SqlCommand(query, conn);
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                conn.Open();
                return cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                Logger.LogException(ex, "DatabaseHelper.ExecuteScalar", ConnectionString);
                throw new ApplicationException("A database error occurred while processing your request. Please try again or contact your administrator.", ex);
            }
        }

        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            try
            {
                using var conn = GetConnection();
                using var cmd = new SqlCommand(query, conn);
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                conn.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Logger.LogException(ex, "DatabaseHelper.ExecuteNonQuery", ConnectionString);
                throw new ApplicationException("A database error occurred while updating data. Please try again or contact your administrator.", ex);
            }
        }
    }
}
