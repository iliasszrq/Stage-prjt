namespace SecureApi.Shared.Documents;
public record CreateDocumentRequest(string Title, string Content);
public record DocumentResponse(Guid Id, string OwnerId, string Title, string Content, DateTime CreatedAt);