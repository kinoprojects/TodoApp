using TodoApi.Models;

namespace TodoApi.Dtos;

// APIの受け取り専用クラスとして作成する
public class CreateTodoRequest
{
    // どのTeamに属するTodoか
    public int TeamId { get; set; }

    // Todoのタイトル
    public string Title { get; set; } = string.Empty;

    // Todoの状態。指定がなければTodoが入る
    public TodoStatus Status { get; set; } = TodoStatus.Todo;

    // Memberの一覧を受け取る。複数人受け取れるようにする
    public List<int> MemberIds { get; set; } = new();
}

