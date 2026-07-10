using TodoApi.Models;

namespace TodoApi.Dtos;

public sealed class CreateTeamMemberRequest
{
    public Position Position { get; set; } = Position.Member;
}
