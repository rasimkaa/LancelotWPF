using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace LancelotWPF
{
    public static class DB
    {
        // ── Поменяйте строку подключения под свой SQL Server ──
        public static string ConnectionString =
            @"Server=DESKTOP-U94P9TD\SQLEXPRESS;Database=LancelotDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static DataTable Query(string sql, params (string, object)[] parameters)
        {
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            foreach (var (name, val) in parameters)
                cmd.Parameters.AddWithValue(name, val ?? DBNull.Value);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public static int Execute(string sql, params (string, object)[] parameters)
        {
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            foreach (var (name, val) in parameters)
                cmd.Parameters.AddWithValue(name, val ?? DBNull.Value);
            return cmd.ExecuteNonQuery();
        }

        public static object? Scalar(string sql, params (string, object)[] parameters)
        {
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            foreach (var (name, val) in parameters)
                cmd.Parameters.AddWithValue(name, val ?? DBNull.Value);
            return cmd.ExecuteScalar();
        }
    }
}
