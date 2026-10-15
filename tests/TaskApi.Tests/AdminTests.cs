using System.Net;
using System.Net.Http.Json;
using TaskApi.Auth;

namespace TaskApi.Tests;

public class AdminTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Member_is_forbidden_from_admin_routes()
    {
        var member = await api.NewMemberAsync();

        var res = await member.GetAsync("/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_lists_users_without_password_hashes()
    {
        var admin = await api.AdminAsync();

        var body = await admin.GetStringAsync("/admin/users");

        Assert.Contains(ApiFactory.AdminEmail, body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase); // spec S5
    }

    [Fact]
    public async Task Admin_can_promote_a_member()
    {
        var admin = await api.AdminAsync();
        var member = await api.NewMemberAsync();
        var me = await member.GetFromJsonAsync<UserResponse>("/auth/me");

        var bad = await admin.PutAsJsonAsync($"/admin/users/{me!.Id}/role", new { role = "SuperUser" });
        var ok = await admin.PutAsJsonAsync($"/admin/users/{me.Id}/role", new { role = Roles.Admin });
        var missing = await admin.PutAsJsonAsync("/admin/users/999999/role", new { role = Roles.Admin });

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Roles.Admin, (await ok.Content.ReadFromJsonAsync<UserResponse>())!.Role);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
