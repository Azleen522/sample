using Microsoft.Data.SqlClient;
using WattWatch.Models;

namespace WattWatch.Data;

public class UserRepository
{
    public User? GetByEmail(string email)
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(@"
            SELECT u.UserId, u.FullName, u.Email, u.PasswordHash, u.PasswordSalt,
                   u.RoleId, r.RoleName, u.IsActive
            FROM Users u JOIN Roles r ON r.RoleId = u.RoleId
            WHERE u.Email = @e", c);
        cmd.Parameters.AddWithValue("@e", email);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return null;
        return new User
        {
            UserId = rd.GetInt32(0), FullName = rd.GetString(1), Email = rd.GetString(2),
            PasswordHash = rd.GetString(3), PasswordSalt = rd.GetString(4),
            RoleId = rd.GetInt32(5), RoleName = rd.GetString(6), IsActive = rd.GetBoolean(7)
        };
    }

    /// <summary>Returns new UserId, or -1 if the email already exists (unique constraint).</summary>
    public int Create(string fullName, string email, string hash, string salt, string roleName)
    {
        try
        {
            using var c = Db.Open();
            using var cmd = new SqlCommand(@"
                INSERT INTO Users(FullName, Email, PasswordHash, PasswordSalt, RoleId)
                OUTPUT INSERTED.UserId
                VALUES (@n, @e, @h, @s, (SELECT RoleId FROM Roles WHERE RoleName = @r))", c);
            cmd.Parameters.AddWithValue("@n", fullName);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@h", hash);
            cmd.Parameters.AddWithValue("@s", salt);
            cmd.Parameters.AddWithValue("@r", roleName);
            return (int)cmd.ExecuteScalar()!;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601) { return -1; }
    }

    public bool AnyAdminExists()
    {
        using var c = Db.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM Users u JOIN Roles r ON r.RoleId=u.RoleId WHERE r.RoleName='Admin'", c);
        return (int)cmd.ExecuteScalar()! > 0;
    }
}
