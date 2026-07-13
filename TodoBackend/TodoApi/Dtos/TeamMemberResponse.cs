namespace TodoApi.Dtos;

public sealed class TeamMemberResponse
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int MemberId { get; set; }
    public string Position { get; set; } = string.Empty;
}
