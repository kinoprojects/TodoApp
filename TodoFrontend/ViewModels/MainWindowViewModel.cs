using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoFrontend.Models;
using TodoFrontend.Services;

namespace TodoFrontend.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /*
     ViewModelは「画面の状態」と「画面からの操作」を担当する。
     HTTPのURLや送信方法までここに書くと、画面の都合と通信の都合が混ざってしまう。
     そのため、API通信はITodoApiClientに任せ、ViewModelは「Todoを取得して」と依頼するだけにしている。
    */
    private readonly ITodoApiClient apiClient;

    /*
     allTodosはAPIから取得したTodoの元リスト。
     Todosは画面に表示するリスト。
     分けておくと、絞り込みを解除したときにAPIへ再取得しなくても元の一覧に戻せる。
    */
    private readonly List<TodoItemViewModel> allTodos = new();

    [ObservableProperty]
    private UserControl? currentView;

    /*
     ErrorMessageは「ユーザーに見せるエラー文」。
     HasErrorMessageなどは、その文があるかどうかから作る画面制御用の状態。
     [NotifyPropertyChangedFor]を付けることで、ErrorMessageが変わったときに関連する表示状態も更新される。
    */
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowErrorState))]
    [NotifyPropertyChangedFor(nameof(IsTodoListVisible))]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowErrorState))]
    [NotifyPropertyChangedFor(nameof(IsTodoListVisible))]
    private bool isLoading;

    /*
     画面側で複雑な条件式を書かないため、ViewModel側で表示状態を名前付きプロパティにしている。
     IsTodoListVisibleはエラー有無を条件に入れていない。
     理由は、追加や削除に失敗しても、すでに表示できているTodo一覧は消さない方がユーザーに親切だから。
    */
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool ShowEmptyState => !IsLoading && !HasErrorMessage && Todos.Count == 0;
    public bool ShowErrorState => !IsLoading && HasErrorMessage && Todos.Count == 0;
    public bool IsTodoListVisible => !IsLoading && Todos.Count > 0;

    /*
     ObservableCollectionは追加・削除を画面に通知できるコレクション。
     List<T>ではなくこれを使うことで、Todos.AddやTodos.ClearがListBoxへ反映される。
    */
    public ObservableCollection<TodoItemViewModel> Todos { get; } = new();
    public ObservableCollection<ProjectResponse> Projects { get; } = new();
    public ObservableCollection<MemberResponse> Members { get; } = new();
    public ObservableCollection<TeamResponse> Teams { get; } = new();

    [ObservableProperty]
    private string newTodoTitle = string.Empty;

    [ObservableProperty]
    private string newTodoTeamName = string.Empty;

    [ObservableProperty]
    private string newTodoMemberNames = string.Empty;

    [ObservableProperty]
    private string newProjectName = string.Empty;

    [ObservableProperty]
    private string newTeamProjectName = string.Empty;

    [ObservableProperty]
    private string newTeamName = string.Empty;

    [ObservableProperty]
    private string newMemberName = string.Empty;

    [ObservableProperty]
    private string filterProjectName = string.Empty;

    [ObservableProperty]
    private string filterTeamName = string.Empty;

    public MainWindowViewModel(ITodoApiClient apiClient, bool createInitialView = true)
    {
        this.apiClient = apiClient;

        /*
         コンストラクタでは通信を始めない。
         ViewModelを作るだけでHTTP通信が走ると、デザイン時表示やテストでもAPIが必要になってしまう。
         ここでは最初に表示するViewを決めるだけにして、実データ取得はInitializeAsyncで明示的に行う。
         テストでは画面部品を作らず状態だけを確認したいので、createInitialViewで切り替えられるようにしている。
         本来は画面遷移も専用サービスに分けるとさらにテストしやすいが、今は学習段階なので小さな切り替えにしている。
        */
        if (createInitialView)
        {
            CurrentView = new TodoListView
            {
                DataContext = this
            };
        }
    }

    public async Task InitializeAsync()
    {
        /*
         初期データ取得はWindowが開いた後に呼ばれる。
         画面側に「読み込み中」を出せるよう、通信前後でIsLoadingを切り替える。
        */
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            /*
             ここは順番にawaitしている。
             複数の取得処理を同時に走らせると、ある処理が失敗してErrorMessageを入れた直後に、
             別の処理が成功してエラーを消す、といった状態の競合が起きやすい。
            */
            await LoadProjectsAsync();
            await LoadTeamsAsync();
            await LoadMembersAsync();
            await LoadTodosAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "初期データを取得できませんでした。API が起動しているか確認してください。";
            Console.WriteLine($"初期データの取得に失敗しました: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            NotifyTodoListStateChanged();
        }
    }

    private async Task LoadTodosAsync()
    {
        var todos = await apiClient.GetTodosAsync();

        allTodos.Clear();

        foreach (var todo in todos)
        {
            /*
             APIから返るTodoResponseを、そのまま画面に出すのではなくTodoItemViewModelへ詰め替える。
             APIの形と画面表示の形を分けると、API側の都合が変わっても画面側の変更を小さくしやすい。
            */
            allTodos.Add(new TodoItemViewModel
            {
                Id = todo.Id,
                Title = todo.Title,
                Status = todo.Status,
                ProjectName = todo.ProjectName ?? "Project未設定",
                TeamName = todo.TeamName ?? $"TeamId: {todo.TeamId}",
                MemberNames = todo.Members.Count == 0
                    ? "担当なし"
                    : string.Join(", ", todo.Members.Select(m => m.MemberName))
            });
        }

        ApplyTodoFilter();
    }

    private async Task LoadProjectsAsync()
    {
        var projects = await apiClient.GetProjectsAsync();

        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(project);
        }
    }

    private async Task LoadTeamsAsync()
    {
        var teams = await apiClient.GetTeamsAsync();

        Teams.Clear();
        foreach (var team in teams)
        {
            Teams.Add(team);
        }
    }

    private async Task LoadMembersAsync()
    {
        var members = await apiClient.GetMembersAsync();

        Members.Clear();
        foreach (var member in members)
        {
            Members.Add(member);
        }
    }

    [RelayCommand]
    private async Task AddTodoAsync()
    {
        /*
         入力チェックの失敗も画面に見せたい状態なので、Console.WriteLineだけにしない。
         ユーザーが次に何を直せばいいか分かるよう、ErrorMessageへ入れる。
        */
        if (string.IsNullOrWhiteSpace(NewTodoTitle))
        {
            ErrorMessage = "タイトルを入力してください。";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTodoTeamName))
        {
            ErrorMessage = "Team名を入力してください。";
            return;
        }

        var team = Teams.FirstOrDefault(t => t.Name == NewTodoTeamName);

        if (team is null)
        {
            ErrorMessage = $"指定されたTeamが見つかりません: {NewTodoTeamName}";
            return;
        }

        var memberIds = new List<int>();

        if (!string.IsNullOrWhiteSpace(NewTodoMemberNames))
        {
            var memberNames = NewTodoMemberNames.Split(',');

            foreach (var memberNameText in memberNames)
            {
                var memberName = memberNameText.Trim();

                if (string.IsNullOrWhiteSpace(memberName))
                {
                    continue;
                }

                var member = Members.FirstOrDefault(m => m.Name == memberName);

                if (member is null)
                {
                    ErrorMessage = $"指定されたMemberが見つかりません: {memberName}";
                    return;
                }

                memberIds.Add(member.Id);
            }
        }

        var request = new CreateTodoRequest
        {
            TeamId = team.Id,
            Title = NewTodoTitle,
            Status = "Todo",
            MemberIds = memberIds
        };

        try
        {
            /*
             成功する可能性のある操作を始めるタイミングで、古いエラーを消す。
             ただし、失敗したらcatchで新しいエラーを入れる。
            */
            ErrorMessage = null;
            await apiClient.AddTodoAsync(request);

            NewTodoTitle = string.Empty;
            NewTodoTeamName = string.Empty;
            NewTodoMemberNames = string.Empty;

            /*
             追加後はAPIから一覧を取り直す。
             手元のリストに1件足すだけより、サーバー側で決まったIdや関連情報を正として画面に戻せる。
            */
            await LoadTodosAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Todoを追加できませんでした。API が起動しているか、入力内容が正しいか確認してください。";
            Console.WriteLine($"Todo追加に失敗しました: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowTodoList()
    {
        /*
         画面切り替えはCurrentViewを差し替えるだけにしている。
         DataContextを同じViewModelにすることで、一覧画面と管理画面が同じ状態を共有できる。
        */
        CurrentView = new TodoListView
        {
            DataContext = this
        };
    }

    [RelayCommand]
    private void ShowManagement()
    {
        CurrentView = new ManagementView
        {
            DataContext = this
        };
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName))
        {
            ErrorMessage = "Project名を入力してください。";
            return;
        }

        var request = new CreateProjectRequest
        {
            Name = NewProjectName
        };

        try
        {
            ErrorMessage = null;
            await apiClient.AddProjectAsync(request);

            NewProjectName = string.Empty;
            await LoadProjectsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Projectを追加できませんでした。API が起動しているか、入力内容が正しいか確認してください。";
            Console.WriteLine($"Project追加に失敗しました: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddTeamAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTeamProjectName))
        {
            ErrorMessage = "Project名を入力してください。";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTeamName))
        {
            ErrorMessage = "Team名を入力してください。";
            return;
        }

        var project = Projects.FirstOrDefault(p => p.Name == NewTeamProjectName);

        if (project is null)
        {
            ErrorMessage = $"指定されたProjectが見つかりません: {NewTeamProjectName}";
            return;
        }

        var request = new CreateTeamRequest
        {
            ProjectId = project.Id,
            Name = NewTeamName
        };

        try
        {
            ErrorMessage = null;
            await apiClient.AddTeamAsync(request);

            NewTeamProjectName = string.Empty;
            NewTeamName = string.Empty;
            await LoadTeamsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Teamを追加できませんでした。API が起動しているか、入力内容が正しいか確認してください。";
            Console.WriteLine($"Team追加に失敗しました: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddMemberAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMemberName))
        {
            ErrorMessage = "Member名を入力してください。";
            return;
        }

        var request = new CreateMemberRequest
        {
            Name = NewMemberName
        };

        try
        {
            ErrorMessage = null;
            await apiClient.AddMemberAsync(request);

            NewMemberName = string.Empty;
            await LoadMembersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Memberを追加できませんでした。API が起動しているか、入力内容が正しいか確認してください。";
            Console.WriteLine($"Member追加に失敗しました: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteTodoAsync(int todoId)
    {
        try
        {
            ErrorMessage = null;
            await apiClient.DeleteTodoAsync(todoId);
            await LoadTodosAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Todoを削除できませんでした。API が起動しているか確認してください。";
            Console.WriteLine($"Todo削除に失敗しました: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UpdateTodoAsync(int todoId)
    {
        var request = new UpdateTodoStatusRequest
        {
            Status = "Done"
        };

        try
        {
            ErrorMessage = null;
            await apiClient.UpdateTodoStatusAsync(todoId, request);
            await LoadTodosAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Todoのステータスを更新できませんでした。API が起動しているか確認してください。";
            Console.WriteLine($"Status更新に失敗しました: {ex.Message}");
        }
    }

    private void ApplyTodoFilter()
    {
        /*
         絞り込みは表示用のTodosだけを作り直す。
         allTodosを直接削らないことで、クリアしたときに元の一覧へ戻せる。
        */
        var filteredTodos = allTodos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FilterProjectName))
        {
            filteredTodos = filteredTodos.Where(todo =>
                todo.ProjectName.Contains(FilterProjectName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterTeamName))
        {
            filteredTodos = filteredTodos.Where(todo =>
                todo.TeamName.Contains(FilterTeamName, StringComparison.OrdinalIgnoreCase));
        }

        Todos.Clear();

        foreach (var todo in filteredTodos)
        {
            Todos.Add(todo);
        }

        NotifyTodoListStateChanged();
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        ApplyTodoFilter();
    }

    [RelayCommand]
    private void ClearFilter()
    {
        FilterProjectName = string.Empty;
        FilterTeamName = string.Empty;

        ApplyTodoFilter();
    }

    private void NotifyTodoListStateChanged()
    {
        /*
         Todos.Countを元にした表示状態は、Todosの中身が変わっただけでは自動通知されない。
         そのため、一覧を作り直した後に関連する状態を明示的に通知している。
        */
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowErrorState));
        OnPropertyChanged(nameof(IsTodoListVisible));
    }
}
