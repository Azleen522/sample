using WattWatch.Data;
using WattWatch.Events;

namespace WattWatch.Handlers;

/// <summary>Reacts to readings; decides whether a NEW violation happened.</summary>
public class EnergyThresholdHandler
{
    private readonly EnergyRepository _energy;
    private readonly AlertRepository _alerts;
    private readonly IEventDispatcher _events;

    public EnergyThresholdHandler(EnergyRepository energy, AlertRepository alerts, IEventDispatcher events)
    { _energy = energy; _alerts = alerts; _events = events; }

    public void Handle(EnergyReadingReceivedEvent e)
    {
        var t = _energy.GetThreshold(e.DeviceId);
        if (t is not { IsEnabled: true } || e.PowerKW <= t.MaxPowerKW) return;
        if (_alerts.HasOpenAlert(e.DeviceId)) return;   // no duplicate alerts

        _events.Publish(new EnergyThresholdExceededEvent(e.ReadingId, e.DeviceId, e.PowerKW, t.MaxPowerKW));
    }
}

/// <summary>Persists the alert in SQL Server.</summary>
public class AlertHandler
{
    private readonly AlertRepository _alerts;
    public AlertHandler(AlertRepository alerts) => _alerts = alerts;

    public void Handle(EnergyThresholdExceededEvent e) =>
        _alerts.Insert(e.DeviceId, e.ReadingId, "ThresholdExceeded",
            $"Power {e.PowerKW:0.###} kW exceeded limit {e.MaxPowerKW:0.###} kW.");
}

/// <summary>Raises plain C# events the dashboard can subscribe to.</summary>
public class NotificationHandler
{
    public static event Action<EnergyThresholdExceededEvent>? AlertRaised;
    public static event Action? AlertsChanged;

    public void Handle(EnergyThresholdExceededEvent e) => AlertRaised?.Invoke(e);
    public void Handle(AlertAcknowledgedEvent e) => AlertsChanged?.Invoke();
}

/// <summary>Writes every important event into AuditLogs.</summary>
public class AuditLogHandler
{
    private readonly AuditLogRepository _audit;
    public AuditLogHandler(AuditLogRepository audit) => _audit = audit;

    public void Handle(UserRegisteredEvent e) => _audit.Add(e.UserId, "UserRegistered", e.Email);
    public void Handle(UserLoggedInEvent e) => _audit.Add(e.UserId, "UserLoggedIn", e.Email);
    public void Handle(EnergyReadingReceivedEvent e) =>
        _audit.Add(e.UserId, "EnergyReadingReceived", $"Device {e.DeviceId}: {e.PowerKW} kW (reading {e.ReadingId})");
    public void Handle(EnergyThresholdExceededEvent e) =>
        _audit.Add(null, "EnergyThresholdExceeded", $"Device {e.DeviceId}: {e.PowerKW} > {e.MaxPowerKW} kW");
    public void Handle(AlertAcknowledgedEvent e) => _audit.Add(e.UserId, "AlertAcknowledged", $"Alert {e.AlertId}");
    public void Handle(ThresholdUpdatedEvent e) =>
        _audit.Add(e.UserId, "ThresholdUpdated", $"Device {e.DeviceId} -> {e.NewMaxPowerKW} kW");
}
