namespace TodoApi.Dtos;

public sealed class TodoResponse
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
