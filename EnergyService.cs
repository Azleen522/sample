using WattWatch.Data;
using WattWatch.Events;
using WattWatch.Models;
using WattWatch.Security;

namespace WattWatch.Services;

public class EnergyService
{
    private readonly EnergyRepository _repo;
    private readonly AlertRepository _alerts;
    private readonly SessionManager _session;
    private readonly AuthorizationService _auth;
    private readonly IEventDispatcher _events;

    public EnergyService(EnergyRepository repo, AlertRepository alerts, SessionManager session,
                         AuthorizationService auth, IEventDispatcher events)
    { _repo = repo; _alerts = alerts; _session = session; _auth = auth; _events = events; }

    public Result RecordReading(int deviceId, decimal powerKw, decimal energyKwh)
    {
        if (!_auth.Can(Permission.SubmitReading)) return Result.Fail("Not authorized.");
        if (powerKw < 0 || energyKwh < 0) return Result.Fail("Values cannot be negative.");

        long id = _repo.InsertReading(deviceId, powerKw, energyKwh);
        // The service does NOT check thresholds or create alerts. It only announces the fact.
        _events.Publish(new EnergyReadingReceivedEvent(id, deviceId, powerKw, _session.UserId));
        return Result.Ok();
    }

    public Result UpdateThreshold(int deviceId, decimal maxKw)
    {
        if (!_auth.Can(Permission.SetThresholds)) return Result.Fail("Admins only.");
        if (maxKw <= 0) return Result.Fail("Limit must be greater than zero.");
        _repo.UpsertThreshold(deviceId, maxKw);
        _events.Publish(new ThresholdUpdatedEvent(deviceId, maxKw, _session.UserId!.Value));
        return Result.Ok();
    }

    public Result AcknowledgeAlert(long alertId)
    {
        if (!_auth.Can(Permission.AcknowledgeAlert)) return Result.Fail("Not authorized.");
        if (!_alerts.Acknowledge(alertId, _session.UserId!.Value))
            return Result.Fail("Alert not found or already acknowledged.");
        _events.Publish(new AlertAcknowledgedEvent(alertId, _session.UserId.Value));
        return Result.Ok();
    }

    public Result ResolveAlert(long alertId)
    {
        if (!_auth.Can(Permission.ManageDevices)) return Result.Fail("Admins only.");
        return _alerts.Resolve(alertId) ? Result.Ok() : Result.Fail("Alert not found.");
    }
}
