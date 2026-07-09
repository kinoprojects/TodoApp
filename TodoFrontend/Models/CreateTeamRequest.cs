namespace TodoFrontend.Models;

public class CreateTeamRequest
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
}