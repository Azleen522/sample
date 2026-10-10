using Microsoft.Data.SqlClient;

namespace WattWatch.Data;

public class AuditLogRepository
{
    public void Add(int? userId, string action, string? details = null)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(
            "INSERT INTO AuditLogs(UserId, Action, Details) VALUES(@u, @a, @d)", c);
        cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@a", action);
        cmd.Parameters.AddWithValue("@d", (object?)details ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}
