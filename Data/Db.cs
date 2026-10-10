using System;
using MySql.Data.MySqlClient;

namespace HospitalSystem.Data
{
    // Repositories ask this for a connection instead of knowing where the database lives,
    // so a test or another environment can point them somewhere else.
    public interface IDbConnectionFactory
    {
        MySqlConnection OpenConnection();
    }

    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        public const string DefaultConnectionString =
            "Server=localhost;Port=3306;Database=hospital_system;Uid=root;Pwd=;";

        private readonly string connectionString;

        public MySqlConnectionFactory(string connectionString = DefaultConnectionString)
        {
            this.connectionString = connectionString;
        }

        public MySqlConnection OpenConnection()
        {
            var conn = new MySqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }

    // Small helpers shared by the repositories.
    internal static class Sql
    {
        public static string ReadString(MySqlDataReader r, string column) =>
            r.IsDBNull(r.GetOrdinal(column)) ? null : r.GetString(column);

        public static object NullIfBlank(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        public static object OrNull(object value) => value ?? DBNull.Value;

        public static string Truncate(string s, int max) =>
            s == null || s.Length <= max ? s : s.Substring(0, max);
    }
}
