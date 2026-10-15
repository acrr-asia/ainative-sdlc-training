using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TaskApi.Tests;

/// A clock the test moves by hand, so lockouts expire without sleeping.
public class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}

/// Same API, plus a controllable clock and an X-Test-Ip header that sets the caller's IP.
public class ThrottleFactory : ApiFactory
{
    public ManualTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IStartupFilter, TestIpStartupFilter>();
        });
    }

    private class TestIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((ctx, nxt) =>
            {
                if (ctx.Request.Headers.TryGetValue("X-Test-Ip", out var ip))
                    ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip!);
                return nxt(ctx);
            });
            next(app);
        };
    }
}

public class LoginThrottleTests(ThrottleFactory api) : IClassFixture<ThrottleFactory>
{
    private static int _ip;
    private static string NewIp() => $"10.0.0.{Interlocked.Increment(ref _ip)}";

    private Task<HttpResponseMessage> Login(string ip, string email, string password)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/auth/login") { Content = JsonContent.Create(new { email, password }) };
        req.Headers.Add("X-Test-Ip", ip);
        return api.CreateClient().SendAsync(req);
    }

    private async Task FailTimes(string ip, int times)
    {
        for (var i = 0; i < times; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(ip, ApiFactory.AdminEmail, "wrong-password")).StatusCode);
    }

    [Fact]
    public async Task Fifth_failure_locks_the_ip_out_with_429_and_retry_after()
    {
        var ip = NewIp();
        await FailTimes(ip, 5);

        var res = await Login(ip, ApiFactory.AdminEmail, "wrong-password");

        Assert.Equal(HttpStatusCode.TooManyRequests, res.StatusCode);
        Assert.True(res.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task Locked_out_ip_gets_429_even_with_the_right_password()
    {
        // spec S6
        var ip = NewIp();
        await FailTimes(ip, 5);

        var res = await Login(ip, ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.TooManyRequests, res.StatusCode);
    }

    [Fact]
    public async Task Lockout_response_is_the_same_for_known_and_unknown_emails()
    {
        // spec S7
        var ip = NewIp();
        await FailTimes(ip, 5);

        var known = await (await Login(ip, ApiFactory.AdminEmail, "x")).Content.ReadFromJsonAsync<ProblemDetails>();
        var unknown = await (await Login(ip, "nobody@test.local", "x")).Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(known!.Detail, unknown!.Detail);
    }

    [Fact]
    public async Task Lockout_ends_after_the_lockout_period()
    {
        var ip = NewIp();
        await FailTimes(ip, 5);

        api.Clock.Advance(TimeSpan.FromSeconds(61));
        var res = await Login(ip, ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Successful_login_clears_the_failure_count()
    {
        var ip = NewIp();
        await FailTimes(ip, 4);
        Assert.Equal(HttpStatusCode.OK, (await Login(ip, ApiFactory.AdminEmail, ApiFactory.AdminPassword)).StatusCode);

        await FailTimes(ip, 4); // would be 8 without the reset

        Assert.Equal(HttpStatusCode.OK, (await Login(ip, ApiFactory.AdminEmail, ApiFactory.AdminPassword)).StatusCode);
    }

    [Fact]
    public async Task Failures_older_than_the_window_do_not_count()
    {
        var ip = NewIp();
        await FailTimes(ip, 4);

        api.Clock.Advance(TimeSpan.FromSeconds(61));
        await FailTimes(ip, 1);

        Assert.Equal(HttpStatusCode.OK, (await Login(ip, ApiFactory.AdminEmail, ApiFactory.AdminPassword)).StatusCode);
    }

    [Fact]
    public async Task One_ips_lockout_does_not_affect_another_ip()
    {
        // spec S9
        var attacker = NewIp();
        await FailTimes(attacker, 5);

        var res = await Login(NewIp(), ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Other_routes_are_never_throttled()
    {
        // spec S8
        var ip = NewIp();
        await FailTimes(ip, 5);

        var req = new HttpRequestMessage(HttpMethod.Post, "/auth/register")
        {
            Content = JsonContent.Create(new { email = $"u-{Guid.NewGuid():N}@test.local", password = "Password123" }),
        };
        req.Headers.Add("X-Test-Ip", ip);
        var res = await api.CreateClient().SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }
}
