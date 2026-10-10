using System.Net.Mail;
using WattWatch.Data;
using WattWatch.Events;
using WattWatch.Models;
using WattWatch.Security;

namespace WattWatch.Services;

public class AuthService
{
    private readonly UserRepository _users;
    private readonly SessionManager _session;
    private readonly IEventDispatcher _events;

    public AuthService(UserRepository users, SessionManager session, IEventDispatcher events)
    { _users = users; _session = session; _events = events; }

    public Result Register(string fullName, string email, string password, string confirm)
    {
        fullName = fullName.Trim(); email = email.Trim().ToLowerInvariant();
        if (fullName.Length == 0) return Result.Fail("Full name is required.");
        if (!IsValidEmail(email)) return Result.Fail("Enter a valid email address.");
        if (password.Length < 8) return Result.Fail("Password must be at least 8 characters.");
        if (password != confirm) return Result.Fail("Passwords do not match.");

        var (hash, salt) = PasswordHasher.Hash(password);
        // Public signup ALWAYS gets the User role.
        int id = _users.Create(fullName, email, hash, salt, "User");
        if (id == -1) return Result.Fail("That email is already registered.");

        _events.Publish(new UserRegisteredEvent(id, email));
        return Result.Ok();
    }

    public Result Login(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        var user = _users.GetByEmail(email);
        // Same generic message for unknown email / wrong password / inactive.
        const string generic = "Invalid email or password.";
        if (user == null || !user.IsActive) return Result.Fail(generic);
        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt)) return Result.Fail(generic);

        _session.SignIn(user);
        _events.Publish(new UserLoggedInEvent(user.UserId, user.Email));
        return Result.Ok();
    }

    public void Logout() => _session.SignOut();

    /// <summary>Controlled setup path for the first admin (not exposed in the UI).</summary>
    public void SeedAdmin(string email, string password)
    {
        if (_users.AnyAdminExists()) return;
        var (hash, salt) = PasswordHasher.Hash(password);
        _users.Create("Administrator", email.ToLowerInvariant(), hash, salt, "Admin");
    }

    private static bool IsValidEmail(string e)
    {
        try { return new MailAddress(e).Address == e; } catch { return false; }
    }
}
