using WattWatch.Data;
using WattWatch.Models;
using WattWatch.Security;
using WattWatch.Services;

namespace WattWatch.Controllers;

public class AuthController
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;
    public Result Signup(string name, string email, string pw, string confirm) => _auth.Register(name, email, pw, confirm);
    public Result Login(string email, string pw) => _auth.Login(email, pw);
    public void Logout() => _auth.Logout();
}

public class EnergyController
{
    private readonly EnergyService _svc; private readonly EnergyRepository _repo;
    private readonly AuthorizationService _authz; private readonly Random _rng = new();

    public EnergyController(EnergyService svc, EnergyRepository repo, AuthorizationService authz)
    { _svc = svc; _repo = repo; _authz = authz; }

    public List<Device> GetDevices() => _authz.Can(Permission.ViewReadings) ? _repo.GetDevices() : new();
    public List<EnergyReading> GetRecent(int deviceId) => _authz.Can(Permission.ViewReadings) ? _repo.GetRecent(deviceId) : new();
    public Result Submit(int deviceId, decimal kw, decimal kwh) => _svc.RecordReading(deviceId, kw, kwh);

    /// <summary>Simulator: random reading between min and max kW; cumulative energy grows each time.</summary>
    public Result Simulate(int deviceId, double minKw = 2, double maxKw = 8)
    {
        decimal kw = Math.Round((decimal)(minKw + _rng.NextDouble() * (maxKw - minKw)), 3);
        var last = _repo.GetRecent(deviceId, 1).FirstOrDefault();
        return _svc.RecordReading(deviceId, kw, (last?.EnergyKWh ?? 0) + kw);
    }
    public Result SetThreshold(int deviceId, decimal kw) => _svc.UpdateThreshold(deviceId, kw);
}

public class AlertController
{
    private readonly EnergyService _svc; private readonly AlertRepository _repo; private readonly AuthorizationService _authz;
    public AlertController(EnergyService svc, AlertRepository repo, AuthorizationService authz)
    { _svc = svc; _repo = repo; _authz = authz; }

    public List<Alert> GetAlerts(bool onlyOpen = false) => _authz.Can(Permission.ViewDashboard) ? _repo.GetAll(onlyOpen) : new();
    public Result Acknowledge(long id) => _svc.AcknowledgeAlert(id);
    public Result Resolve(long id) => _svc.ResolveAlert(id);
}
