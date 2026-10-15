using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;

namespace TaskApi.Auth;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var email = config["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = config["SeedAdmin:Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        var admin = new User { Email = email, Role = Roles.Admin, PasswordHash = "" };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
