using WattWatch.Controllers;
using WattWatch.Data;
using WattWatch.Events;
using WattWatch.Handlers;
using WattWatch.Security;
using WattWatch.Services;
using WattWatch.Views;

namespace WattWatch;

/// <summary>Simple composition root shared with the forms.</summary>
public static class AppContext2
{
    public static AuthController Auth = null!;
    public static EnergyController Energy = null!;
    public static AlertController Alerts = null!;
    public static SessionManager Session = null!;
    public static AuthorizationService Authz = null!;
}

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Infrastructure
        var events = new EventDispatcher();
        var session = new SessionManager();
        var authz = new AuthorizationService(session);
        var users = new UserRepository(); var energyRepo = new EnergyRepository();
        var alertRepo = new AlertRepository(); var auditRepo = new AuditLogRepository();

        // Services
        var authSvc = new AuthService(users, session, events);
        var energySvc = new EnergyService(energyRepo, alertRepo, session, authz, events);

        // Controlled first-admin setup:  WattWatch.exe --seed-admin  (password from env var)
        if (args.Contains("--seed-admin"))
        {
            var pw = Environment.GetEnvironmentVariable("WATTWATCH_ADMIN_PASSWORD");
            if (string.IsNullOrEmpty(pw)) { MessageBox.Show("Set WATTWATCH_ADMIN_PASSWORD first."); return; }
            authSvc.SeedAdmin("admin@wattwatch.local", pw);
            MessageBox.Show("Admin seeded: admin@wattwatch.local");
            return;
        }

        // Event subscriptions: the heart of the EDA
        var threshold = new EnergyThresholdHandler(energyRepo, alertRepo, events);
        var alertH = new AlertHandler(alertRepo);
        var notify = new NotificationHandler();
        var audit = new AuditLogHandler(auditRepo);

        events.Subscribe<EnergyReadingReceivedEvent>(threshold.Handle);
        events.Subscribe<EnergyReadingReceivedEvent>(audit.Handle);
        events.Subscribe<EnergyThresholdExceededEvent>(alertH.Handle);   // subscribed before notify so the alert exists when UI refreshes
        events.Subscribe<EnergyThresholdExceededEvent>(notify.Handle);
        events.Subscribe<EnergyThresholdExceededEvent>(audit.Handle);
        events.Subscribe<AlertAcknowledgedEvent>(audit.Handle);
        events.Subscribe<AlertAcknowledgedEvent>(notify.Handle);
        events.Subscribe<ThresholdUpdatedEvent>(audit.Handle);
        events.Subscribe<UserRegisteredEvent>(audit.Handle);
        events.Subscribe<UserLoggedInEvent>(audit.Handle);

        // Controllers
        AppContext2.Session = session; AppContext2.Authz = authz;
        AppContext2.Auth = new AuthController(authSvc);
        AppContext2.Energy = new EnergyController(energySvc, energyRepo, authz);
        AppContext2.Alerts = new AlertController(energySvc, alertRepo, authz);

        Application.Run(new LoginForm());
    }
}
