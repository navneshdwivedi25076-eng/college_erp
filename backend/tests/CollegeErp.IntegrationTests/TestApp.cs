using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CollegeErp.IntegrationTests;

/// <summary>Boots the real API against a throw-away SQLite data folder.</summary>
public sealed class TestApp : WebApplicationFactory<Program>
{
    public string DataDir { get; } = Path.Combine(Path.GetTempPath(), "college-erp-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(DataDir);
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:MasterConnectionString", $"Data Source={Path.Combine(DataDir, "master.db")}");
        builder.UseSetting("Database:SqliteDirectory", Path.Combine(DataDir, "colleges"));
        builder.UseSetting("Bootstrap:SuperAdminEmail", "root@test.local");
        builder.UseSetting("Bootstrap:SuperAdminPassword", "RootPass123");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(DataDir, recursive: true); } catch (IOException) { }
    }

    public HttpClient Client(string? token = null)
    {
        var c = CreateClient();
        if (token != null) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    public async Task<string> PlatformTokenAsync() =>
        await LoginAsync("/api/platform/auth/login", "root@test.local", "RootPass123");

    public async Task<string> CollegeTokenAsync(string code, string email, string password) =>
        await LoginAsync($"/api/t/{code}/auth/login", email, password);

    private async Task<string> LoginAsync(string url, string email, string password)
    {
        var res = await Client().PostAsJsonAsync(url, new { email, password });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }

    /// <summary>Creates a college via the platform API and returns its admin token.</summary>
    public async Task<(JsonElement College, string AdminToken)> CreateCollegeAsync(string code, int? maxStudents = null)
    {
        var platform = Client(await PlatformTokenAsync());
        var res = await platform.PostAsJsonAsync("/api/platform/colleges", new
        {
            code, name = $"College {code}", maxStudents,
            adminEmail = $"admin@{code}.edu", adminFullName = "Admin", adminPassword = "Passw0rd!",
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, res.StatusCode);
        var college = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (college, await CollegeTokenAsync(code, $"admin@{code}.edu", "Passw0rd!"));
    }
}

public static class HttpExtensions
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {body}");
        return string.IsNullOrEmpty(body) ? default : JsonDocument.Parse(body).RootElement.Clone();
    }

    public static int Id(this JsonElement e) => e.GetProperty("id").GetInt32();
}
