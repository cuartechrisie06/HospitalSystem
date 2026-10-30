using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data.Repositories
{
    public interface IAlertRepository
    {
        // Open alerts only (Active or Acknowledged); resolved ones stay in the table as history.
        List<Alert> GetOpen();
        void Insert(Alert a);

        // Title, message, severity, status and acknowledged-by.
        void Update(Alert a);

        // Status, acknowledged-by and resolved time.
        void UpdateStatus(Alert a);
    }

    public interface IActivityLogRepository
    {
        void Insert(ActivityItem item);
        List<ActivityItem> Query(DateTime? from, DateTime? to, string module, string username, string search, int limit);
        List<string> Modules();
    }

    public class MySqlAlertRepository : IAlertRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlAlertRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Alert> GetOpen()
        {
            var list = new List<Alert>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT id, title, message, severity, status, created_on, source_key, module, acknowledged_by " +
                "FROM alerts WHERE status IN ('Active', 'Acknowledged')", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string sourceKey = Sql.ReadString(r, "source_key");
                    list.Add(new Alert
                    {
                        Id = r.GetInt32("id"),
                        Title = Sql.ReadString(r, "title") ?? "",
                        Message = Sql.ReadString(r, "message") ?? "",
                        Severity = Sql.ReadString(r, "severity") ?? "Low",
                        Status = r.GetString("status"),
                        CreatedOn = r.GetDateTime("created_on"),
                        SourceKey = sourceKey,
                        Module = Sql.ReadString(r, "module"),
                        AcknowledgedBy = Sql.ReadString(r, "acknowledged_by"),
                        IsAuto = sourceKey != null
                    });
                }
            }
            return list;
        }

        public void Insert(Alert a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO alerts (title, message, severity, status, created_on, source_key, module) " +
                "VALUES (@title, @message, @severity, @status, @createdOn, @sourceKey, @module); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@title", a.Title);
                cmd.Parameters.AddWithValue("@message", Sql.Truncate(a.Message, 500));
                cmd.Parameters.AddWithValue("@severity", a.Severity);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@createdOn", a.CreatedOn);
                cmd.Parameters.AddWithValue("@sourceKey", a.SourceKey);
                cmd.Parameters.AddWithValue("@module", a.Module);
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(Alert a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE alerts SET title=@title, message=@message, severity=@severity, status=@status, acknowledged_by=@ackBy WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@title", a.Title);
                cmd.Parameters.AddWithValue("@message", Sql.Truncate(a.Message, 500));
                cmd.Parameters.AddWithValue("@severity", a.Severity);
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@ackBy", Sql.OrNull(a.AcknowledgedBy));
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(Alert a)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE alerts SET status=@status, acknowledged_by=@ackBy, resolved_on=@resolvedOn WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@status", a.Status);
                cmd.Parameters.AddWithValue("@ackBy", Sql.OrNull(a.AcknowledgedBy));
                cmd.Parameters.AddWithValue("@resolvedOn", Sql.OrNull(a.ResolvedOn));
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class MySqlActivityLogRepository : IActivityLogRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlActivityLogRepository(IDbConnectionFactory db) { this.db = db; }

        public void Insert(ActivityItem item)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO activity_log (module, action, description, icon, created_at, username) " +
                "VALUES (@module, @action, @description, @icon, @createdAt, @username); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@module", item.Module);
                cmd.Parameters.AddWithValue("@action", item.Action);
                cmd.Parameters.AddWithValue("@description", Sql.Truncate(item.Description, 255));
                cmd.Parameters.AddWithValue("@icon", (object)item.Icon ?? "");
                cmd.Parameters.AddWithValue("@createdAt", item.Timestamp);
                cmd.Parameters.AddWithValue("@username", Sql.Truncate(item.Username, 50));
                item.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Newest first. Every filter is optional; limit keeps a huge log from freezing the grid.
        public List<ActivityItem> Query(DateTime? from, DateTime? to, string module, string username, string search, int limit)
        {
            var sql = new StringBuilder(
                "SELECT id, module, action, description, icon, created_at, username FROM activity_log WHERE 1=1");
            var cmd = new MySqlCommand();
            if (from.HasValue) { sql.Append(" AND created_at >= @from"); cmd.Parameters.AddWithValue("@from", from.Value); }
            if (to.HasValue) { sql.Append(" AND created_at < @to"); cmd.Parameters.AddWithValue("@to", to.Value); }
            if (!string.IsNullOrEmpty(module)) { sql.Append(" AND module = @module"); cmd.Parameters.AddWithValue("@module", module); }
            if (!string.IsNullOrEmpty(username)) { sql.Append(" AND username = @username"); cmd.Parameters.AddWithValue("@username", username); }
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql.Append(" AND (description LIKE @search OR action LIKE @search)");
                cmd.Parameters.AddWithValue("@search", "%" + search.Trim() + "%");
            }
            sql.Append(" ORDER BY created_at DESC, id DESC LIMIT @limit");
            cmd.Parameters.AddWithValue("@limit", limit);

            var list = new List<ActivityItem>();
            using (var conn = db.OpenConnection())
            using (cmd)
            {
                cmd.Connection = conn;
                cmd.CommandText = sql.ToString();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ActivityItem
                        {
                            Id = r.GetInt32("id"),
                            Module = r.GetString("module"),
                            Action = r.GetString("action"),
                            Description = r.GetString("description"),
                            Icon = Sql.ReadString(r, "icon") ?? "",
                            Timestamp = r.GetDateTime("created_at"),
                            Username = Sql.ReadString(r, "username") ?? "(not recorded)"
                        });
                    }
                }
            }
            return list;
        }

        public List<string> Modules()
        {
            var list = new List<string>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand("SELECT DISTINCT module FROM activity_log ORDER BY module", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(r.GetString(0));
            }
            return list;
        }
    }
}
