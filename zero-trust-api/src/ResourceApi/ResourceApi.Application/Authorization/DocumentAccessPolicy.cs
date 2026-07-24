using ResourceApi.Domain.Entities;

namespace ResourceApi.Application.Authorization;
public static class DocumentAccessPolicy
{
       public static bool CanAccess(string userId, Document document)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        return document.OwnerId == userId;
    }
}