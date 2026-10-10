using Microsoft.Data.SqlClient;

namespace WattWatch.Data;

public static class Db
{
    // Integrated security: no SQL passwords stored in the app.
    public const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=WattWatchDB;Integrated Security=true;TrustServerCertificate=true";

    public static SqlConnection Open()
    {
        var c = new SqlConnection(ConnectionString);
        c.Open();
        return c;
    }
}
