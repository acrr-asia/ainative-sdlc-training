using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskApi.Auth;
using TaskApi.Data;

namespace TaskApi.Tasks;

public record CreateTaskRequest(string Title, int? CategoryId);
public record UpdateTaskRequest(string Title, bool IsDone, int? CategoryId);

public static class TaskEndpoints
{
    public static RouteGroupBuilder MapTasks(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks").WithTags("Tasks");

        group.MapGet("/", List);
        group.MapGet("/{id:int}", Get);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);

        return group;
    }

    private static async Task<IResult> List(ClaimsPrincipal user, AppDbContext db)
    {
        var query = db.Tasks.AsQueryable();
        if (!user.IsInRole(Roles.Admin))
            query = query.Where(t => t.OwnerId == user.UserId());
        return Results.Ok(await query.OrderBy(t => t.Id).ToListAsync());
    }

    private static async Task<IResult> Get(int id, ClaimsPrincipal user, AppDbContext db, IAuthorizationService auth) =>
        await FindAuthorized(id, user, db, auth) is { } task ? Results.Ok(task) : Results.NotFound();

    private static async Task<IResult> Create(CreateTaskRequest req, ClaimsPrincipal user, AppDbContext db)
    {
        if (await Validate(req.Title, req.CategoryId, db) is { } problem) return problem;

        var task = new TaskItem
        {
            Title = req.Title.Trim(),
            CategoryId = req.CategoryId,
            OwnerId = user.UserId(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return Results.Created($"/tasks/{task.Id}", task);
    }

    private static async Task<IResult> Update(
        int id, UpdateTaskRequest req, ClaimsPrincipal user, AppDbContext db, IAuthorizationService auth)
    {
        if (await FindAuthorized(id, user, db, auth) is not { } task) return Results.NotFound();
        if (await Validate(req.Title, req.CategoryId, db) is { } problem) return problem;

        task.Title = req.Title.Trim();
        task.IsDone = req.IsDone;
        task.CategoryId = req.CategoryId;
        await db.SaveChangesAsync();
        return Results.Ok(task);
    }

    private static async Task<IResult> Delete(int id, ClaimsPrincipal user, AppDbContext db, IAuthorizationService auth)
    {
        if (await FindAuthorized(id, user, db, auth) is not { } task) return Results.NotFound();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // Spec S4: a task the caller may not touch looks exactly like a missing task.
    private static async Task<TaskItem?> FindAuthorized(
        int id, ClaimsPrincipal user, AppDbContext db, IAuthorizationService auth)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return null;
        var result = await auth.AuthorizeAsync(user, task, Policies.TaskOwnerOrAdmin);
        return result.Succeeded ? task : null;
    }

    private static async Task<IResult?> Validate(string? title, int? categoryId, AppDbContext db)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmed = title?.Trim() ?? "";
        if (trimmed.Length is 0 or > 200) errors["title"] = ["Title must be 1 to 200 characters."];
        if (categoryId is { } cid && !await db.Categories.AnyAsync(c => c.Id == cid))
            errors["categoryId"] = ["Category does not exist."];
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }
}
