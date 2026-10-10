using Microsoft.Data.SqlClient;
using WattWatch.Models;

namespace WattWatch.Data;

public class EnergyRepository
{
    public long InsertReading(int deviceId, decimal powerKw, decimal energyKwh)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(@"
            INSERT INTO EnergyReadings(DeviceId, PowerKW, EnergyKWh)
            OUTPUT INSERTED.ReadingId VALUES (@d, @p, @e)", c);
        cmd.Parameters.AddWithValue("@d", deviceId);
        cmd.Parameters.AddWithValue("@p", powerKw);
        cmd.Parameters.AddWithValue("@e", energyKwh);
        return (long)cmd.ExecuteScalar()!;
    }

    public List<EnergyReading> GetRecent(int deviceId, int count = 50)
    {
        var list = new List<EnergyReading>();
        using var c = Db.Open();
        using var cmd = new SqlCommand(@"
            SELECT TOP (@n) ReadingId, DeviceId, PowerKW, EnergyKWh, RecordedAt
            FROM EnergyReadings WHERE DeviceId=@d ORDER BY RecordedAt DESC, ReadingId DESC", c);
        cmd.Parameters.AddWithValue("@n", count);
        cmd.Parameters.AddWithValue("@d", deviceId);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
            list.Add(new EnergyReading { ReadingId = rd.GetInt64(0), DeviceId = rd.GetInt32(1),
                PowerKW = rd.GetDecimal(2), EnergyKWh = rd.GetDecimal(3), RecordedAt = rd.GetDateTime(4) });
        return list;
    }

    public List<Device> GetDevices()
    {
        var list = new List<Device>();
        using var c = Db.Open();
        using var cmd = new SqlCommand(
            "SELECT DeviceId, DeviceName, DeviceCode, BuildingId, IsActive FROM Devices WHERE IsActive=1", c);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
            list.Add(new Device { DeviceId = rd.GetInt32(0), DeviceName = rd.GetString(1),
                DeviceCode = rd.GetString(2), BuildingId = rd.GetInt32(3), IsActive = rd.GetBoolean(4) });
        return list;
    }

    public Threshold? GetThreshold(int deviceId)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(
            "SELECT DeviceId, MaxPowerKW, IsEnabled FROM Thresholds WHERE DeviceId=@d", c);
        cmd.Parameters.AddWithValue("@d", deviceId);
        using var rd = cmd.ExecuteReader();
        return rd.Read()
            ? new Threshold { DeviceId = rd.GetInt32(0), MaxPowerKW = rd.GetDecimal(1), IsEnabled = rd.GetBoolean(2) }
            : null;
    }

    public void UpsertThreshold(int deviceId, decimal maxKw)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(@"
            MERGE Thresholds AS t USING (SELECT @d AS DeviceId) s ON t.DeviceId = s.DeviceId
            WHEN MATCHED THEN UPDATE SET MaxPowerKW=@m, UpdatedAt=SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT(DeviceId, MaxPowerKW) VALUES(@d, @m);", c);
        cmd.Parameters.AddWithValue("@d", deviceId);
        cmd.Parameters.AddWithValue("@m", maxKw);
        cmd.ExecuteNonQuery();
    }
}
