using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace TaskApi.Auth;

public class LoginThrottleOptions
{
    public const string Section = "LoginThrottle";
    public int MaxFailures { get; set; } = 5;
    public int WindowSeconds { get; set; } = 60;
    public int LockoutSeconds { get; set; } = 60;
}

// Spec 0002: counts failed logins per client IP, in memory (one instance; see spec section 3).
public class LoginThrottle(IOptions<LoginThrottleOptions> options, TimeProvider time)
{
    private readonly LoginThrottleOptions _opt = options.Value;
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    private record Entry(DateTimeOffset WindowStart, int Failures, DateTimeOffset? LockedUntil);

    public bool IsLockedOut(string ip, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        if (!_entries.TryGetValue(ip, out var e) || e.LockedUntil is not { } until) return false;

        var now = time.GetUtcNow();
        if (now >= until)
        {
            _entries.TryRemove(ip, out _);
            return false;
        }
        retryAfter = until - now;
        return true;
    }

    public void RecordFailure(string ip)
    {
        var now = time.GetUtcNow();
        _entries.AddOrUpdate(ip,
            _ => Next(new Entry(now, 0, null), now),
            (_, e) => Next(now - e.WindowStart > TimeSpan.FromSeconds(_opt.WindowSeconds) ? new Entry(now, 0, null) : e, now));
    }

    public void Reset(string ip) => _entries.TryRemove(ip, out _);

    private Entry Next(Entry e, DateTimeOffset now)
    {
        var failures = e.Failures + 1;
        var lockedUntil = failures >= _opt.MaxFailures ? now.AddSeconds(_opt.LockoutSeconds) : e.LockedUntil;
        return e with { Failures = failures, LockedUntil = lockedUntil };
    }
}
