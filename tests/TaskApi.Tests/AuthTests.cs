using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Auth;

namespace TaskApi.Tests;

public class AuthTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task Register_returns_201_member_without_password_hash()
    {
        var res = await api.CreateClient().PostAsJsonAsync("/auth/register",
            new { email = NewEmail(), password = "Password123" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("\"role\":\"Member\"", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase); // spec S5
    }

    [Fact]
    public async Task Register_duplicate_email_returns_409()
    {
        var client = api.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/auth/register", new { email, password = "Password123" });

        var res = await client.PostAsJsonAsync("/auth/register", new { email = email.ToUpperInvariant(), password = "Password123" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Password123")]
    [InlineData("ok@test.local", "short")]
    public async Task Register_invalid_input_returns_400(string email, string password)
    {
        var res = await api.CreateClient().PostAsJsonAsync("/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var client = await api.AdminAsync();

        var me = await client.GetFromJsonAsync<UserResponse>("/auth/me");

        Assert.Equal(ApiFactory.AdminEmail, me!.Email);
        Assert.Equal(Roles.Admin, me.Role);
    }

    [Fact]
    public async Task Login_unknown_email_and_wrong_password_look_identical()
    {
        var client = api.CreateClient();

        var unknown = await client.PostAsJsonAsync("/auth/login", new { email = NewEmail(), password = "Password123" });
        var wrong = await client.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = "WrongPass123" });

        // spec S3
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        var a = await unknown.Content.ReadFromJsonAsync<ProblemDetails>();
        var b = await wrong.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(a!.Detail, b!.Detail);
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var res = await api.CreateClient().GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [InlineData(ApiFactory.SigningKey, -10)] // expired
    [InlineData("a-different-key-that-is-also-32-bytes-long", 10)] // forged
    public async Task Expired_or_forged_token_returns_401(string key, int minutesFromNow)
    {
        // spec S2
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken("TaskApi", "TaskApi",
            [new Claim("sub", "1"), new Claim("role", Roles.Admin)],
            notBefore: now.AddMinutes(-60), expires: now.AddMinutes(minutesFromNow),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        var res = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
