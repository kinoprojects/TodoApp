using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class CreateMemberRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
