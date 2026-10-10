using WattWatch.Handlers;
using WattWatch.Models;
using WattWatch.Security;

namespace WattWatch.Views;

public class DashboardForm : Form
{
    private readonly ComboBox cmbDevice = new() { Left = 10, Top = 10, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown numKw = new() { Left = 270, Top = 10, Width = 80, DecimalPlaces = 3, Maximum = 100000 };
    private readonly Button btnSubmit = new() { Text = "Submit reading", Left = 360, Top = 9, Width = 110 };
    private readonly Button btnSim = new() { Text = "Simulate", Left = 480, Top = 9, Width = 90 };
    private readonly Button btnAck = new() { Text = "Acknowledge", Left = 580, Top = 9, Width = 100 };
    private readonly Button btnLimit = new() { Text = "Set limit (Admin)", Left = 690, Top = 9, Width = 120 };
    private readonly Button btnLogout = new() { Text = "Logout", Left = 820, Top = 9, Width = 70 };
    private readonly Label lblStats = new() { Left = 10, Top = 45, Width = 880, Height = 22 };
    private readonly DataGridView gridReadings = new() { Left = 10, Top = 75, Width = 430, Height = 400, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly DataGridView gridAlerts = new() { Left = 450, Top = 75, Width = 440, Height = 400, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

    public DashboardForm()
    {
        // Guard: no dashboard without a session.
        if (!AppContext2.Authz.Can(Permission.ViewDashboard)) { Load += (_, _) => Close(); return; }

        Text = $"WattWatch - {AppContext2.Session.Email} ({AppContext2.Session.Role})";
        ClientSize = new Size(900, 485); StartPosition = FormStartPosition.CenterScreen;
        Controls.AddRange(new Control[] { cmbDevice, numKw, btnSubmit, btnSim, btnAck, btnLimit, btnLogout, lblStats, gridReadings, gridAlerts });
        btnLimit.Enabled = AppContext2.Authz.Can(Permission.SetThresholds);   // UX only; real check is in the service

        cmbDevice.DataSource = AppContext2.Energy.GetDevices();
        cmbDevice.SelectedIndexChanged += (_, _) => RefreshAll();
        // "Submit reading" uses the number box as kW; cumulative = latest + kW (same rule as the simulator)
        btnSubmit.Click += (_, _) => Act(AppContext2.Energy.Submit(Dev.DeviceId, numKw.Value, LatestKwh() + numKw.Value));
        btnSim.Click += (_, _) => Act(AppContext2.Energy.Simulate(Dev.DeviceId));
        btnAck.Click += (_, _) =>
        {
            if (gridAlerts.CurrentRow?.DataBoundItem is Alert a) Act(AppContext2.Alerts.Acknowledge(a.AlertId));
        };
        btnLimit.Click += (_, _) => Act(AppContext2.Energy.SetThreshold(Dev.DeviceId, numKw.Value));
        btnLogout.Click += (_, _) => Close();

        // EDA -> UI: the dashboard subscribes to the notification handler's C# events.
        NotificationHandler.AlertRaised += OnAlertRaised;
        NotificationHandler.AlertsChanged += RefreshAlertsSafe;
        FormClosed += (_, _) =>
        {
            NotificationHandler.AlertRaised -= OnAlertRaised;
            NotificationHandler.AlertsChanged -= RefreshAlertsSafe;
        };
        RefreshAll();
    }

    private Device Dev => (Device)cmbDevice.SelectedItem!;
    private decimal LatestKwh() => AppContext2.Energy.GetRecent(Dev.DeviceId).FirstOrDefault()?.EnergyKWh ?? 0;

    private void Act(Result r)
    {
        if (!r.Success) MessageBox.Show(r.Error);
        RefreshAll();
    }

    private void OnAlertRaised(Events.EnergyThresholdExceededEvent e)
    {
        if (IsDisposed) return;
        BeginInvoke(() =>
        {
            RefreshAll();
            MessageBox.Show($"ALERT: {e.PowerKW} kW exceeds limit {e.MaxPowerKW} kW", "WattWatch",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });
    }

    private void RefreshAlertsSafe() { if (!IsDisposed) BeginInvoke(RefreshAll); }

    private void RefreshAll()
    {
        if (cmbDevice.SelectedItem == null) return;
        var readings = AppContext2.Energy.GetRecent(Dev.DeviceId);
        var alerts = AppContext2.Alerts.GetAlerts();
        gridReadings.DataSource = readings;
        gridAlerts.DataSource = alerts;
        var latest = readings.FirstOrDefault();
        lblStats.Text = $"Current power: {latest?.PowerKW ?? 0} kW   |   Cumulative: {latest?.EnergyKWh ?? 0} kWh   |   Open alerts: {alerts.Count(a => a.Status != "Resolved")}";
    }
}
