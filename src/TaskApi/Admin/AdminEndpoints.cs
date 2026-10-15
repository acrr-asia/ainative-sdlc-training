using Microsoft.EntityFrameworkCore;
using TaskApi.Auth;
using TaskApi.Data;

namespace TaskApi.Admin;

public record ChangeRoleRequest(string Role);

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdmin(this IEndpointRouteBuilder app)
    {
        // The whole group is admin-only, so a new route here cannot forget the check.
        var group = app.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/users", async (AppDbContext db) =>
            await db.Users.OrderBy(u => u.Id).Select(u => new UserResponse(u.Id, u.Email, u.Role)).ToListAsync());

        group.MapPut("/users/{id:int}/role", async (int id, ChangeRoleRequest req, AppDbContext db) =>
        {
            if (!Roles.All.Contains(req.Role))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = [$"Role must be one of: {string.Join(", ", Roles.All)}."],
                });
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();
            user.Role = req.Role;
            await db.SaveChangesAsync();
            return Results.Ok(UserResponse.From(user));
        });

        return group;
    }
}
