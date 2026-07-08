using System.Runtime.InteropServices.Marshalling;

namespace TodoFrontend.ViewModels;

public class TodoItemViewModel
{
    public int Id {get; set;}
    public string Title {get; set;} = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string MemberNames { get; set; } = string.Empty;
}