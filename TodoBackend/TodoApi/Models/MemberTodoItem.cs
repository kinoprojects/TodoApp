using System.Text.Json.Serialization;

namespace TodoApi.Models;

public class MemberTodoItem
{
     public int Id { get; set; }

     public int MemberId { get; set; } // Memberへの外部キー

     public int TodoItemId { get; set; } // TodoItemへの外部キー

     public Member? Member { get; set; } // 関連するMember本体を辿るようにする

     [JsonIgnore]
     public TodoItem? TodoItem { get; set; } // 関連するTodoItem本体を辿れるようにする
}