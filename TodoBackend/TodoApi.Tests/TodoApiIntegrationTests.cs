using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TodoApi.Tests;

/*
 APIをHTTP経由で呼ぶ統合テスト。
 ここではController単体ではなく、実際のURLにリクエストしたときのステータスコードやレスポンスを確認する。
 「フロントやPostmanから叩いたときにどう返るか」に近い形でテストできる。
*/
public sealed class TodoApiIntegrationTests
{
    /*
     ASP.NET CoreのJSONはcamelCaseで返る設定になりやすい。
     JsonSerializerDefaults.Webを使うと、テスト側の読み取りもWeb APIの標準的なJSON設定に合わせられる。
    */
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateTodo_WithMissingTeamId_ReturnsBadRequest()
    {
        /*
         Todoは必ずTeamに属する前提。
         存在しないTeamIdで作れてしまうと、後から一覧表示や集計で壊れたデータになる。
         そのため、APIが400 Bad Requestで拒否することを確認している。
        */
        await using var factory = new TodoApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/todos", new
        {
            teamId = 999,
            title = "存在しないTeamのTodo",
            status = "Todo",
            memberIds = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTeam_WithMissingId_ReturnsNotFound()
    {
        /*
         存在しないリソースを取得しようとしたときは404を返す。
         400ではなく404にすることで、「リクエストの形は正しいが対象がない」と区別できる。
        */
        await using var factory = new TodoApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/teams/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithDuplicateName_ReturnsConflict()
    {
        /*
         Project名はユニークにしたい業務ルール。
         2回目の作成が成功してしまうと、同名Projectが並んでユーザーが区別しづらくなる。
         409 Conflictで「既にあるため衝突した」と返すことを確認している。
        */
        await using var factory = new TodoApiTestFactory();
        var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync("api/Projects", new
        {
            name = "重複Project"
        });
        var secondResponse = await client.PostAsJsonAsync("api/Projects", new
        {
            name = "重複Project"
        });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_SetsCreatedAtAndUpdatedAt()
    {
        /*
         CreatedAt / UpdatedAtは履歴や並び替えの基礎になる値。
         作成時にこの2つが入っていないと、後から「いつ作られたか」「いつ更新されたか」を扱えない。
        */
        await using var factory = new TodoApiTestFactory();
        var client = factory.CreateClient();
        var team = await SeedProjectAndTeamAsync(factory);

        var response = await client.PostAsJsonAsync("api/todos", new
        {
            teamId = team.Id,
            title = "日時確認Todo",
            status = "Todo",
            memberIds = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(todo);
        Assert.NotEqual(default, todo.CreatedAt);
        Assert.NotEqual(default, todo.UpdatedAt);
    }

    [Fact]
    public async Task UpdateTodo_ChangesUpdatedAtWithoutChangingCreatedAt()
    {
        /*
         CreatedAtは「作られた時刻」なので更新で変わってはいけない。
         UpdatedAtは「最後に変更された時刻」なので更新で変わるべき。
         この2つの役割が混ざらないことを確認している。
        */
        await using var factory = new TodoApiTestFactory();
        var client = factory.CreateClient();
        var team = await SeedProjectAndTeamAsync(factory);

        var createResponse = await client.PostAsJsonAsync("api/todos", new
        {
            teamId = team.Id,
            title = "更新前Todo",
            status = "Todo",
            memberIds = Array.Empty<int>()
        });
        createResponse.EnsureSuccessStatusCode();

        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(createdTodo);

        /*
         作成直後に更新すると、環境によっては時刻差がほぼ同じになることがある。
         UpdatedAtが進んだことを安定して確認するため、短く待ってから更新している。
        */
        await Task.Delay(20);

        var updateResponse = await client.PutAsJsonAsync($"api/todos/{createdTodo.Id}", new
        {
            title = "更新後Todo",
            status = "Done"
        });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedTodo = await context.TodoItems.FindAsync(createdTodo.Id);

        Assert.NotNull(updatedTodo);
        Assert.Equal(createdTodo.CreatedAt, updatedTodo.CreatedAt);
        Assert.True(updatedTodo.UpdatedAt > createdTodo.UpdatedAt);
    }

    private static async Task<Team> SeedProjectAndTeamAsync(TodoApiTestFactory factory)
    {
        /*
         Todo作成にはTeamが必要で、Team作成にはProjectが必要。
         各テストで同じ準備を何度も書かないよう、このヘルパーで必要最小限のデータを用意する。
        */
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var project = new Project
        {
            Name = $"Project-{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var team = new Team
        {
            Project = project,
            Name = $"Team-{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        return team;
    }
}
