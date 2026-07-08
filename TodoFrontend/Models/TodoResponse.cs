using System;
using HarfBuzzSharp;
using System.Collections.Generic;

namespace TodoFrontend.Models;

// APIから返ってくるJSONを受け止めるためのクラス。APIから返ってきた情報のなかでここに記載のあるものだけ受け取るようになっている
public class TodoResponse
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<TodoMemberResponse> Members { get; set; } = new();
}