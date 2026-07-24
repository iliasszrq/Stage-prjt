namespace ResourceApi.Domain.Entities;
public class Document
{
    public required Guid Id{ get; init; }
    public required string OwnerId{get; init;}
    public required string Title{get; set;}
    public required string Content { get; set;}
   public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
}