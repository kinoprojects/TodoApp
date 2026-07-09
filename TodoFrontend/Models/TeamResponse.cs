namespace TodoFrontend.Models;

public class TeamResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
}