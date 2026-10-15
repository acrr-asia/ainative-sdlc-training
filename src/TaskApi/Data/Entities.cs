namespace TaskApi.Data;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class TaskItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsDone { get; set; }
    public int OwnerId { get; set; }
    public int? CategoryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
