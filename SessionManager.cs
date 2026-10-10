using WattWatch.Models;

namespace WattWatch.Security;

public class SessionManager
{
    public int? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SignIn(User u) { UserId = u.UserId; Email = u.Email; Role = u.RoleName; }
    public void SignOut() { UserId = null; Email = null; Role = null; }
}
