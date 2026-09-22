using MySql.Data.MySqlClient;

namespace HospitalSystem.Data
{
    public static class Db
    {
        public const string ConnectionString =
            "Server=localhost;Port=3306;Database=hospital_system;Uid=root;Pwd=;";

        public static MySqlConnection OpenConnection()
        {
            var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
