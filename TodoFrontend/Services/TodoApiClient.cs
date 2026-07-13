using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TodoFrontend.Models;

namespace TodoFrontend.Services;

/*
 TodoApiClientはHTTP通信の具体的な実装を担当するクラス。
 ViewModelにURLやGet/Postの書き方を置かないことで、
 画面の状態管理とAPI通信の責務を分けている。
*/
public class TodoApiClient : ITodoApiClient
{
    private readonly HttpClient httpClient;

    public TodoApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<TodoResponse>> GetTodosAsync()
    {
        /*
         APIがnullを返した場合でも、ViewModel側では「空の一覧」として扱えるようにする。
         こうしておくと、ViewModelで毎回nullチェックを書く必要がなくなる。
        */
        return await httpClient.GetFromJsonAsync<List<TodoResponse>>("api/todos") ?? new();
    }

    public async Task<List<ProjectResponse>> GetProjectsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<ProjectResponse>>("api/Projects") ?? new();
    }

    public async Task<List<TeamResponse>> GetTeamsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<TeamResponse>>("api/teams") ?? new();
    }

    public async Task<List<MemberResponse>> GetMembersAsync()
    {
        return await httpClient.GetFromJsonAsync<List<MemberResponse>>("api/Members") ?? new();
    }

    public async Task AddTodoAsync(CreateTodoRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/todos", request);
        await EnsureSuccessAsync(response);
    }

    public async Task AddProjectAsync(CreateProjectRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/Projects", request);
        await EnsureSuccessAsync(response);
    }

    public async Task AddTeamAsync(CreateTeamRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/teams", request);
        await EnsureSuccessAsync(response);
    }

    public async Task AddMemberAsync(CreateMemberRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/members", request);
        await EnsureSuccessAsync(response);
    }

    public async Task DeleteTodoAsync(int todoId)
    {
        var response = await httpClient.DeleteAsync($"api/todos/{todoId}");
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateTodoStatusAsync(int todoId, UpdateTodoStatusRequest request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/todos/{todoId}/status", request);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        /*
         POST/PUT/DELETEは、通信できてもHTTPステータスが失敗ならアプリとしては失敗。
         ここで例外に変換しておくと、ViewModel側はtry/catchで同じ形のエラー処理にできる。
        */
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"API request failed. StatusCode: {(int)response.StatusCode} {response.StatusCode}. Body: {errorBody}");
    }
}
