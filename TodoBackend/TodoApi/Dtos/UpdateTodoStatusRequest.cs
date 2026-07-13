using TodoApi.Models;

namespace TodoApi.Dtos;

public sealed class UpdateTodoStatusRequest
{
    public TodoStatus Status { get; set; }
}
