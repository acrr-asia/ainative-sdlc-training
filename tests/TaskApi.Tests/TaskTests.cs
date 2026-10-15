using System.Net;
using System.Net.Http.Json;
using TaskApi.Data;

namespace TaskApi.Tests;

public class TaskTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static async Task<TaskItem> CreateTask(HttpClient client, string title = "Write the spec")
    {
        var res = await client.PostAsJsonAsync("/tasks", new { title });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<TaskItem>())!;
    }

    [Fact]
    public async Task Member_sees_only_their_own_tasks()
    {
        var alice = await api.NewMemberAsync();
        var bob = await api.NewMemberAsync();
        var aliceTask = await CreateTask(alice, "Alice's task");
        await CreateTask(bob, "Bob's task");

        var tasks = await alice.GetFromJsonAsync<List<TaskItem>>("/tasks");

        Assert.Single(tasks!);
        Assert.Equal(aliceTask.Id, tasks![0].Id);
    }

    [Fact]
    public async Task Owner_can_complete_and_delete_their_task()
    {
        var member = await api.NewMemberAsync();
        var task = await CreateTask(member);

        var update = await member.PutAsJsonAsync($"/tasks/{task.Id}", new { title = "Done!", isDone = true });
        var updated = await update.Content.ReadFromJsonAsync<TaskItem>();
        var delete = await member.DeleteAsync($"/tasks/{task.Id}");
        var get = await member.GetAsync($"/tasks/{task.Id}");

        Assert.True(updated!.IsDone);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Other_member_gets_404_not_403_on_someone_elses_task()
    {
        // spec S4
        var owner = await api.NewMemberAsync();
        var intruder = await api.NewMemberAsync();
        var task = await CreateTask(owner);

        var get = await intruder.GetAsync($"/tasks/{task.Id}");
        var put = await intruder.PutAsJsonAsync($"/tasks/{task.Id}", new { title = "hijacked", isDone = true });
        var delete = await intruder.DeleteAsync($"/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var stillThere = await owner.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
        Assert.Equal("Write the spec", stillThere!.Title);
    }

    [Fact]
    public async Task Admin_can_see_and_edit_any_task()
    {
        var member = await api.NewMemberAsync();
        var admin = await api.AdminAsync();
        var task = await CreateTask(member);

        var all = await admin.GetFromJsonAsync<List<TaskItem>>("/tasks");
        var update = await admin.PutAsJsonAsync($"/tasks/{task.Id}", new { title = "Reviewed", isDone = false });

        Assert.Contains(all!, t => t.Id == task.Id);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("valid title", 999_999)]
    public async Task Invalid_title_or_unknown_category_returns_400(string title, int? categoryId)
    {
        var member = await api.NewMemberAsync();

        var res = await member.PostAsJsonAsync("/tasks", new { title, categoryId });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Task_can_use_an_admin_created_category()
    {
        var admin = await api.AdminAsync();
        var member = await api.NewMemberAsync();
        var cat = await (await admin.PostAsJsonAsync("/categories", new { name = $"work-{Guid.NewGuid():N}"[..20] }))
            .Content.ReadFromJsonAsync<Category>();

        var res = await member.PostAsJsonAsync("/tasks", new { title = "Ship it", categoryId = cat!.Id });
        var task = await res.Content.ReadFromJsonAsync<TaskItem>();

        Assert.Equal(cat.Id, task!.CategoryId);
    }
}
