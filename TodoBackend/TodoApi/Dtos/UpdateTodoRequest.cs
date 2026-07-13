using System.ComponentModel.DataAnnotations;
using TodoApi.Models;

namespace TodoApi.Dtos;

public sealed class UpdateTodoRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public TodoStatus Status { get; set; }
}
