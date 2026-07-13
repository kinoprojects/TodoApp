using System.Collections.Generic;
using System.Threading.Tasks;
using TodoFrontend.Models;

namespace TodoFrontend.Services;

/*
 API通信の「できること」をViewModelへ見せるための窓口。
 ViewModelはこのinterfaceだけを知っていればよいので、HttpClientやURLの細かい扱いから離れられる。
 将来テストを書くときも、このinterfaceの偽物を渡せば実APIなしでViewModelを確認できる。
 TodoFrontend.Testsでは実際にFakeTodoApiClientを渡して、ViewModelだけをテストしている。
*/
public interface ITodoApiClient
{
    Task<List<TodoResponse>> GetTodosAsync();
    Task<List<ProjectResponse>> GetProjectsAsync();
    Task<List<TeamResponse>> GetTeamsAsync();
    Task<List<MemberResponse>> GetMembersAsync();
    Task AddTodoAsync(CreateTodoRequest request);
    Task AddProjectAsync(CreateProjectRequest request);
    Task AddTeamAsync(CreateTeamRequest request);
    Task AddMemberAsync(CreateMemberRequest request);
    Task DeleteTodoAsync(int todoId);
    Task UpdateTodoStatusAsync(int todoId, UpdateTodoStatusRequest request);
}
