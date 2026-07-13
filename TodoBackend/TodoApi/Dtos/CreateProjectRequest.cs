using System.ComponentModel.DataAnnotations;
using TodoApi.Models;

namespace TodoApi.Dtos;

// DTOは受け取る内容、送る内容を決めるだけのものなので、継承されて別プロパティとかが追加できないようにする
public sealed class CreateProjectRequest
{
    // 受け取る内容をNameのみに限定する
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}