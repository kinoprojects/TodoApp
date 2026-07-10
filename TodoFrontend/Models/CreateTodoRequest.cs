

using System.Collections.Generic;

namespace TodoFrontend.Models;

public class CreateTodoRequest
{
    public int TeamId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = "Todo";

    public List<int> MemberIds { get; set; } = new();
}