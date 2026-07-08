namespace TodoApi.Models;

public class Project
{
    public int Id { get; set; } // ID

    public string Name { get; set; } = string.Empty; //プロジェクト名

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 作成日時

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // 更新日時

    public List<Team> Teams { get; set; } = new(); // projectに紐づくチームをこちらからも辿れるように追加しておく
}