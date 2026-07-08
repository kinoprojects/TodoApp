namespace TodoApi.Models;

public class TodoItem
{
    public int Id { get; set; } //ID

    public int TeamId { get; set; } // teamモデルのID

    public string Title { get; set; } = string.Empty; //タイトル nullを避ける

    public TodoStatus Status { get; set; } = TodoStatus.Todo; //タスクのステータス

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //作成日時

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // 更新日時

    public Team? Team { get; set; } //チーム本体を辿るためのプロパティ

    public List<MemberTodoItem> MemberTodoItems { get; set; } = new();  // タスクに紐づくメンバー情報を辿れるようにする
}