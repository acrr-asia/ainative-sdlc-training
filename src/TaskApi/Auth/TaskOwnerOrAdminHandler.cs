using Microsoft.AspNetCore.Authorization;
using TaskApi.Data;

namespace TaskApi.Auth;

public class TaskOwnerOrAdminRequirement : IAuthorizationRequirement;

// Resource-based policy: the endpoint loads the task, then asks this handler.
public class TaskOwnerOrAdminHandler : AuthorizationHandler<TaskOwnerOrAdminRequirement, TaskItem>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TaskOwnerOrAdminRequirement requirement, TaskItem task)
    {
        if (context.User.IsInRole(Roles.Admin) || task.OwnerId == context.User.UserId())
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
