using System.Text.Json.Serialization;

namespace TodoApi.Models;

public class Team
{
    public int Id { get; set; } // ID

    public int ProjectId { get; set; } // プロジェクトモデルのID

    public string Name { get; set; } = string.Empty; // チーム名

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //作成日時

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // 更新日時

    [JsonIgnore]
    public Project? Project { get; set; } // 関連するProject本体の情報を扱うためのプロパティ

    [JsonIgnore]
    public List<TodoItem> TodoItems { get; set; } = new(); // チームに紐づくタスクを辿れる様に追加しておく

    public List<TeamMember> TeamMembers { get; set; } = new(); 

}