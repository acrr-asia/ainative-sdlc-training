using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskApi.Auth;

namespace TaskApi.Tests;

/// Boots the real API in memory against a throwaway SQLite file.
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "AdminPass123";
    public const string SigningKey = "test-signing-key-that-is-at-least-32-bytes";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"taskapi-test-{Guid.NewGuid():N}.db");

    public string? SigningKeyOverride { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath};Pooling=False");
        builder.UseSetting("Jwt:SigningKey", SigningKeyOverride ?? SigningKey);
        builder.UseSetting("SeedAdmin:Email", AdminEmail);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);
    }

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
        var token = await res.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public Task<HttpClient> AdminAsync() => LoginAsync(AdminEmail, AdminPassword);

    /// Registers a brand-new member with a unique email and returns a signed-in client.
    public async Task<HttpClient> NewMemberAsync()
    {
        var email = $"member-{Guid.NewGuid():N}@test.local";
        const string password = "MemberPass123";
        var res = await CreateClient().PostAsJsonAsync("/auth/register", new { email, password });
        res.EnsureSuccessStatusCode();
        return await LoginAsync(email, password);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
