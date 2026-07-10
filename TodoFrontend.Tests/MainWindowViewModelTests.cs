using TodoFrontend.Models;
using TodoFrontend.Services;
using TodoFrontend.ViewModels;
using Xunit;

namespace TodoFrontend.Tests;

/*
 MainWindowViewModelだけを確認するテスト。
 実HTTP通信や実APIサーバーは使わず、ITodoApiClientの偽物を渡す。
 これにより「API通信が成功/失敗したとき、ViewModelの画面状態がどう変わるか」だけに集中できる。
*/
public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task InitializeAsync_WhenApiReturnsTodos_AddsDisplayTodos()
    {
        /*
         APIがTodoResponseを返したとき、
         ViewModelが画面表示用のTodoItemViewModelへ詰め替えられるかを確認する。
         これはMVVMでいう「Model/APIの形」と「画面表示の形」を分ける部分のテスト。
        */
        var apiClient = new FakeTodoApiClient
        {
            Todos =
            [
                new TodoResponse
                {
                    Id = 1,
                    TeamId = 10,
                    TeamName = "開発Team",
                    ProjectName = "TodoApp",
                    Title = "テストを書く",
                    Status = "Todo",
                    Members =
                    [
                        new TodoMemberResponse
                        {
                            MemberId = 1,
                            MemberName = "山田"
                        }
                    ]
                }
            ]
        };
        /*
         createInitialView: falseにしている理由:
         このテストで確認したいのは画面部品ではなくViewModelの状態。
         TodoListViewを作るとAvaloniaのUI初期化が必要になるため、状態テストでは作らない。
        */
        var viewModel = new MainWindowViewModel(apiClient, createInitialView: false);

        await viewModel.InitializeAsync();

        Assert.Single(viewModel.Todos);

        var todo = viewModel.Todos[0];
        Assert.Equal(1, todo.Id);
        Assert.Equal("テストを書く", todo.Title);
        Assert.Equal("Todo", todo.Status);
        Assert.Equal("TodoApp", todo.ProjectName);
        Assert.Equal("開発Team", todo.TeamName);
        Assert.Equal("山田", todo.MemberNames);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_WhenApiFails_SetsErrorMessage()
    {
        /*
         APIが例外を投げたとき、ViewModelがユーザー向けのErrorMessageへ変換できるかを確認する。
         例外そのものを画面に出すのではなく、画面に出してよい日本語メッセージにするのがViewModelの役割。
        */
        var apiClient = new FakeTodoApiClient
        {
            GetTodosException = new InvalidOperationException("API failed")
        };
        var viewModel = new MainWindowViewModel(apiClient, createInitialView: false);

        await viewModel.InitializeAsync();

        Assert.Equal("初期データを取得できませんでした。API が起動しているか確認してください。", viewModel.ErrorMessage);
        Assert.True(viewModel.HasErrorMessage);
        Assert.True(viewModel.ShowErrorState);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhileApiIsRunning_SetsIsLoadingTrue()
    {
        /*
         IsLoadingは「今まさに読み込み中か」を表す画面状態。
         普通のfakeだとすぐ完了してしまい、読み込み中の瞬間を確認しづらい。
         BlockingTodoApiClientでAPI応答を一時停止し、その間にIsLoadingがtrueになることを確認する。
        */
        var apiClient = new BlockingTodoApiClient();
        var viewModel = new MainWindowViewModel(apiClient, createInitialView: false);

        var initializeTask = viewModel.InitializeAsync();
        await apiClient.GetProjectsStarted.Task;

        Assert.True(viewModel.IsLoading);

        apiClient.CompleteRequests();
        await initializeTask;

        Assert.False(viewModel.IsLoading);
    }

    private class FakeTodoApiClient : ITodoApiClient
    {
        /*
         テスト用の偽物APIクライアント。
         本物のTodoApiClientはHTTP通信をするが、このfakeはメモリ上の値を返すだけ。
         こうするとAPIサーバーやネットワークに依存せず、ViewModelだけを速く安定してテストできる。
        */
        public List<TodoResponse> Todos { get; init; } = new();
        public Exception? GetTodosException { get; init; }

        public virtual Task<List<TodoResponse>> GetTodosAsync()
        {
            if (GetTodosException is not null)
            {
                throw GetTodosException;
            }

            return Task.FromResult(Todos);
        }

        public virtual Task<List<ProjectResponse>> GetProjectsAsync()
        {
            return Task.FromResult(new List<ProjectResponse>());
        }

        public virtual Task<List<TeamResponse>> GetTeamsAsync()
        {
            return Task.FromResult(new List<TeamResponse>());
        }

        public virtual Task<List<MemberResponse>> GetMembersAsync()
        {
            return Task.FromResult(new List<MemberResponse>());
        }

        public virtual Task AddTodoAsync(CreateTodoRequest request)
        {
            return Task.CompletedTask;
        }

        public virtual Task AddProjectAsync(CreateProjectRequest request)
        {
            return Task.CompletedTask;
        }

        public virtual Task AddTeamAsync(CreateTeamRequest request)
        {
            return Task.CompletedTask;
        }

        public virtual Task AddMemberAsync(CreateMemberRequest request)
        {
            return Task.CompletedTask;
        }

        public virtual Task DeleteTodoAsync(int todoId)
        {
            return Task.CompletedTask;
        }

        public virtual Task UpdateTodoStatusAsync(int todoId, UpdateTodoStatusRequest request)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingTodoApiClient : FakeTodoApiClient
    {
        /*
         読み込み中の状態をテストするため、あえてAPI応答を止めるfake。
         TaskCompletionSourceは「テスト側が好きなタイミングで非同期処理を完了させる」ために使う。
        */
        private readonly TaskCompletionSource requestsCanFinish = new();

        public TaskCompletionSource GetProjectsStarted { get; } = new();

        public override async Task<List<ProjectResponse>> GetProjectsAsync()
        {
            GetProjectsStarted.TrySetResult();
            await requestsCanFinish.Task;
            return new List<ProjectResponse>();
        }

        public void CompleteRequests()
        {
            requestsCanFinish.TrySetResult();
        }
    }
}
