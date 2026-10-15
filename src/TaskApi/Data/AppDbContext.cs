using Microsoft.EntityFrameworkCore;

namespace TaskApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Email).HasMaxLength(256);
        b.Entity<Category>().HasIndex(c => c.Name).IsUnique();
        b.Entity<Category>().Property(c => c.Name).HasMaxLength(50);
        b.Entity<TaskItem>().Property(t => t.Title).HasMaxLength(200);
        b.Entity<TaskItem>().HasOne<User>().WithMany().HasForeignKey(t => t.OwnerId);
        b.Entity<TaskItem>().HasOne<Category>().WithMany().HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
