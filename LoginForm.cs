namespace WattWatch.Views;

// Built in code so it works without the designer. Replace with designer controls if you prefer.
public class LoginForm : Form
{
    private readonly TextBox txtEmail = new() { PlaceholderText = "Email", Left = 20, Top = 20, Width = 260 };
    private readonly TextBox txtPassword = new() { PlaceholderText = "Password", UseSystemPasswordChar = true, Left = 20, Top = 55, Width = 260 };
    private readonly Button btnLogin = new() { Text = "Log in", Left = 20, Top = 90, Width = 260 };
    private readonly Label lblError = new() { ForeColor = Color.Firebrick, Left = 20, Top = 125, Width = 260, Height = 30 };
    private readonly LinkLabel linkSignup = new() { Text = "Create an account", Left = 20, Top = 160, Width = 260 };

    public LoginForm()
    {
        Text = "WattWatch - Login"; ClientSize = new Size(300, 195);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        Controls.AddRange(new Control[] { txtEmail, txtPassword, btnLogin, lblError, linkSignup });
        AcceptButton = btnLogin;
        btnLogin.Click += (_, _) => DoLogin();
        linkSignup.LinkClicked += (_, _) => { using var f = new SignupForm(); f.ShowDialog(this); };
    }

    private void DoLogin()
    {
        var r = AppContext2.Auth.Login(txtEmail.Text, txtPassword.Text);
        if (!r.Success) { lblError.Text = r.Error; return; }
        txtPassword.Clear();
        Hide();
        using (var dash = new DashboardForm()) dash.ShowDialog();   // returns on logout
        AppContext2.Auth.Logout();
        lblError.Text = ""; Show();
    }
}
