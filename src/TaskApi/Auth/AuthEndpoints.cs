using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;

namespace TaskApi.Auth;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record UserResponse(int Id, string Email, string Role)
{
    public static UserResponse From(User u) => new(u.Id, u.Email, u.Role);
}

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new UserResponse(
            user.UserId(),
            user.FindFirstValue("email") ?? "",
            user.FindFirstValue("role") ?? "")));

        return group;
    }

    private static async Task<IResult> Register(RegisterRequest req, AppDbContext db, IPasswordHasher<User> hasher)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        var errors = new Dictionary<string, string[]>();
        if (email.Length is 0 or > 256 || !email.Contains('@')) errors["email"] = ["A valid email is required."];
        if ((req.Password?.Length ?? 0) < 8) errors["password"] = ["Password must be at least 8 characters."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Results.Problem("Email is already registered.", statusCode: StatusCodes.Status409Conflict);

        var user = new User { Email = email, Role = Roles.Member, PasswordHash = "" };
        user.PasswordHash = hasher.HashPassword(user, req.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Results.Created($"/admin/users/{user.Id}", UserResponse.From(user));
    }

    private static async Task<IResult> Login(
        LoginRequest req, HttpContext http, AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens,
        LoginThrottle throttle)
    {
        // Spec S6/S7: check the lockout before looking at the email or password.
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (throttle.IsLockedOut(ip, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
            return Results.Problem("Too many failed logins. Try again later.", statusCode: StatusCodes.Status429TooManyRequests);
        }

        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Spec S3: one response for "no such user" and "wrong password".
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "")
            == PasswordVerificationResult.Failed)
        {
            throttle.RecordFailure(ip);
            return Results.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        throttle.Reset(ip);
        return Results.Ok(tokens.Issue(user));
    }
}
