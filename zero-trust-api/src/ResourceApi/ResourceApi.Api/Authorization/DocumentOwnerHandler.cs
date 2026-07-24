using Microsoft.AspNetCore.Authorization;
using ResourceApi.Application.Authorization;
using ResourceApi.Domain.Entities;

namespace ResourceApi.Api.Authorization;

public class DocumentOwnerHandler
    : AuthorizationHandler<DocumentOwnerRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOwnerRequirement requirement,
        Document resource)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (userId is not null && DocumentAccessPolicy.CanAccess(userId, resource))
        {
            context.Succeed(requirement);
        }
        // No else: not calling Succeed means the requirement fails.
        // Fail-closed by default.

        return Task.CompletedTask;
    }
}