using Microsoft.Data.SqlClient;
using WattWatch.Models;

namespace WattWatch.Data;

public class AlertRepository
{
    /// <summary>True while the device still has an unresolved alert (prevents duplicates).</summary>
    public bool HasOpenAlert(int deviceId)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM Alerts WHERE DeviceId=@d AND Status <> 'Resolved'", c);
        cmd.Parameters.AddWithValue("@d", deviceId);
        return (int)cmd.ExecuteScalar()! > 0;
    }

    public long Insert(int deviceId, long readingId, string type, string message)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(@"
            INSERT INTO Alerts(DeviceId, ReadingId, AlertType, Message)
            OUTPUT INSERTED.AlertId VALUES(@d, @r, @t, @m)", c);
        cmd.Parameters.AddWithValue("@d", deviceId);
        cmd.Parameters.AddWithValue("@r", readingId);
        cmd.Parameters.AddWithValue("@t", type);
        cmd.Parameters.AddWithValue("@m", message);
        return (long)cmd.ExecuteScalar()!;
    }

    public bool Acknowledge(long alertId, int userId)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(@"
            UPDATE Alerts SET Status='Acknowledged', AcknowledgedBy=@u, AcknowledgedAt=SYSUTCDATETIME()
            WHERE AlertId=@a AND Status='Active'", c);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@a", alertId);
        return cmd.ExecuteNonQuery() == 1;
    }

    public bool Resolve(long alertId)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(
            "UPDATE Alerts SET Status='Resolved' WHERE AlertId=@a AND Status <> 'Resolved'", c);
        cmd.Parameters.AddWithValue("@a", alertId);
        return cmd.ExecuteNonQuery() == 1;
    }

    public List<Alert> GetAll(bool onlyOpen = false)
    {
        var list = new List<Alert>();
        using var c = Db.Open();
        using var cmd = new SqlCommand($@"
            SELECT a.AlertId, a.DeviceId, d.DeviceName, a.ReadingId, a.AlertType, a.Message, a.Status, a.CreatedAt
            FROM Alerts a JOIN Devices d ON d.DeviceId=a.DeviceId
            {(onlyOpen ? "WHERE a.Status <> 'Resolved'" : "")}
            ORDER BY a.CreatedAt DESC", c);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
            list.Add(new Alert { AlertId = rd.GetInt64(0), DeviceId = rd.GetInt32(1), DeviceName = rd.GetString(2),
                ReadingId = rd.GetInt64(3), AlertType = rd.GetString(4), Message = rd.GetString(5),
                Status = rd.GetString(6), CreatedAt = rd.GetDateTime(7) });
        return list;
    }
}
