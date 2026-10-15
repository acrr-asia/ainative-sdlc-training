using Microsoft.EntityFrameworkCore;
using TaskApi.Auth;
using TaskApi.Data;

namespace TaskApi.Categories;

public record CreateCategoryRequest(string Name);

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategories(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").WithTags("Categories");

        // Reading is open to any signed-in user (fallback policy).
        group.MapGet("/", async (AppDbContext db) =>
            await db.Categories.OrderBy(c => c.Name).ToListAsync());

        // Writing is admin-only.
        group.MapPost("/", Create).RequireAuthorization(Policies.AdminOnly);
        group.MapDelete("/{id:int}", Delete).RequireAuthorization(Policies.AdminOnly);

        return group;
    }

    private static async Task<IResult> Create(CreateCategoryRequest req, AppDbContext db)
    {
        var name = req.Name?.Trim() ?? "";
        if (name.Length is 0 or > 50)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Name must be 1 to 50 characters."],
            });
        if (await db.Categories.AnyAsync(c => c.Name == name))
            return Results.Problem("Category already exists.", statusCode: StatusCodes.Status409Conflict);

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Results.Created($"/categories/{category.Id}", category);
    }

    private static async Task<IResult> Delete(int id, AppDbContext db) =>
        await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync() == 0
            ? Results.NotFound()
            : Results.NoContent();
}
