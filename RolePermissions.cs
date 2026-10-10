namespace WattWatch.Security;

public enum Permission
{
    ViewDashboard, ViewReadings, SubmitReading, AcknowledgeAlert,
    ManageDevices, SetThresholds, ManageUsers, ViewAuditLog
}

public class AuthorizationService
{
    private readonly SessionManager _session;
    public AuthorizationService(SessionManager session) => _session = session;

    private static readonly Dictionary<string, HashSet<Permission>> Map = new()
    {
        ["Admin"] = new(Enum.GetValues<Permission>()),
        ["User"]  = new() { Permission.ViewDashboard, Permission.ViewReadings,
                            Permission.SubmitReading, Permission.AcknowledgeAlert }
    };

    public bool Can(Permission p) =>
        _session.IsAuthenticated && _session.Role != null &&
        Map.TryGetValue(_session.Role, out var set) && set.Contains(p);
}
