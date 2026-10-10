namespace WattWatch.Views;

public class SignupForm : Form
{
    private readonly TextBox name = new() { PlaceholderText = "Full name", Left = 20, Top = 20, Width = 260 };
    private readonly TextBox email = new() { PlaceholderText = "Email", Left = 20, Top = 55, Width = 260 };
    private readonly TextBox pw = new() { PlaceholderText = "Password (min 8)", UseSystemPasswordChar = true, Left = 20, Top = 90, Width = 260 };
    private readonly TextBox confirm = new() { PlaceholderText = "Confirm password", UseSystemPasswordChar = true, Left = 20, Top = 125, Width = 260 };
    private readonly Button create = new() { Text = "Create Account", Left = 20, Top = 160, Width = 260 };
    private readonly Label err = new() { ForeColor = Color.Firebrick, Left = 20, Top = 195, Width = 260, Height = 30 };

    public SignupForm()
    {
        Text = "WattWatch - Sign up"; ClientSize = new Size(300, 235);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; StartPosition = FormStartPosition.CenterParent;
        Controls.AddRange(new Control[] { name, email, pw, confirm, create, err });
        create.Click += (_, _) =>
        {
            var r = AppContext2.Auth.Signup(name.Text, email.Text, pw.Text, confirm.Text);
            if (!r.Success) { err.Text = r.Error; return; }
            MessageBox.Show("Account created. Please log in.");
            Close();
        };
    }
}
