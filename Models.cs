namespace WattWatch.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "User";
    public bool IsActive { get; set; }
}

public class Device
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public string DeviceCode { get; set; } = "";
    public int BuildingId { get; set; }
    public bool IsActive { get; set; }
    public override string ToString() => $"{DeviceName} ({DeviceCode})";
}

public class EnergyReading
{
    public long ReadingId { get; set; }
    public int DeviceId { get; set; }
    public decimal PowerKW { get; set; }
    public decimal EnergyKWh { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class Threshold
{
    public int DeviceId { get; set; }
    public decimal MaxPowerKW { get; set; }
    public bool IsEnabled { get; set; }
}

public class Alert
{
    public long AlertId { get; set; }
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public long ReadingId { get; set; }
    public string AlertType { get; set; } = "";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

public class Result
{
    public bool Success { get; init; }
    public string Error { get; init; } = "";
    public static Result Ok() => new() { Success = true };
    public static Result Fail(string error) => new() { Error = error };
}
