using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class CreateTeamRequest
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
