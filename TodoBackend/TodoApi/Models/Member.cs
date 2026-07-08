using System.Text.Json.Serialization;

namespace TodoApi.Models;

public class Member
{
    public int Id { get; set; } // ID

    public string Name { get; set; } = string.Empty; //メンバー名

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 作成日時

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public List<MemberTodoItem> MemberTodoItems { get; set; } = new(); // メンバーに紐づく複数のタスクを辿れるようにする

    [JsonIgnore]
    public List<TeamMember> TeamMembers { get; set; } = new(); // メンバーに紐づくチームメンバーの情報を辿れるようにする
}