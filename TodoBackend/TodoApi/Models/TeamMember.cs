using System.Text.Json.Serialization;

namespace TodoApi.Models;

public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; } // Teamへの外部キー

    public int MemberId { get; set; } // Memberへの外部キー

    public Position Position { get; set; } = Position.Member;  // Positionをenumで定義

    [JsonIgnore]
    public Team? Team { get; set; }


    public Member? Member { get; set; }

}