using System.Net;
using System.Net.Http.Json;
using TaskApi.Data;

namespace TaskApi.Tests;

public class CategoryTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static string NewName() => $"cat-{Guid.NewGuid():N}"[..20];

    [Fact]
    public async Task Member_can_list_but_not_create_categories()
    {
        var member = await api.NewMemberAsync();

        var list = await member.GetAsync("/categories");
        var create = await member.PostAsJsonAsync("/categories", new { name = NewName() });

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_and_delete_category()
    {
        var admin = await api.AdminAsync();

        var create = await admin.PostAsJsonAsync("/categories", new { name = NewName() });
        var category = await create.Content.ReadFromJsonAsync<Category>();
        var delete = await admin.DeleteAsync($"/categories/{category!.Id}");
        var deleteAgain = await admin.DeleteAsync($"/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }

    [Fact]
    public async Task Duplicate_or_empty_category_name_is_rejected()
    {
        var admin = await api.AdminAsync();
        var name = NewName();
        await admin.PostAsJsonAsync("/categories", new { name });

        var duplicate = await admin.PostAsJsonAsync("/categories", new { name });
        var empty = await admin.PostAsJsonAsync("/categories", new { name = "  " });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_delete_category()
    {
        var member = await api.NewMemberAsync();

        var res = await member.DeleteAsync("/categories/1");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
