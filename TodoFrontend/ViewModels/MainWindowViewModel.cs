using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TodoFrontend.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;


namespace TodoFrontend.ViewModels;

/*
 MainWindowViewModelは、画面とデータの間をつなぐクラス。

 このファイルの大きな役割は、既存コメントにもある通り、
 APIからTodo一覧を取得し、画面表示用のTodosコレクションに入れて
 MainWindow.axamlのListBoxに表示させること。

 関連ファイルとのつながり:
 1. MainWindow.axaml
    画面の見た目を書く場所。
    ListBoxのItemsSource="{Binding Todos}" によって、
    このクラスのTodosを画面に表示している。

 2. TodoResponse.cs
    APIから返ってくるJSONを受け止めるための形。
    ここではGetFromJsonAsync<List<TodoResponse>>で使っている。

 3. TodoItemViewModel.cs
    画面に表示しやすい形に整えたTodo 1件分の形。
    APIから受け取ったTodoResponseを、TodoItemViewModelに詰め替えている。

 4. TodoBackend側の /api/todos
    Todo一覧を返すAPI。
    このViewModelはHttpClientを使って、そのAPIへアクセスしている。

 画面開発で迷ったときは、
 「APIから受け取る形」はModel、
 「画面に出す形」はViewModel、
 「実際の見た目」はView
 と考えると立ち返りやすい。
 */
public partial class MainWindowViewModel : ViewModelBase
{
    /*
     HTTP通信をするための道具を作成。
     AvaloniaアプリからASP.NET Core APIにアクセスするために使います。
     それ以外のインスタンスに差し替えたりはしない。

     今回はこの_httpClientを使って、
     バックエンドの http://localhost:5128/api/todos にアクセスしている。
     */
    private readonly HttpClient _httpClient = new();


    [ObservableProperty]
    private UserControl? currentView; // 現在のviewを入れる。public UserControl? CurrentViewが自動生成されるので使える
    /*
     ObservableCollectionは、中身が追加・削除されたことを画面に通知できるListみたいなもの。

     MainWindow.axamlのListBoxはこのTodosを見ている。
     そのため、TodosにTodoItemViewModelを追加すると、画面側にも反映される。

     TodoItemViewModelを複数持てる一覧を空で用意している。
     */
    public ObservableCollection<TodoItemViewModel> Todos { get; } = new();

    // 全権取得用のリスト 
    private List<TodoItemViewModel> allTodos {get;} = new();

    // 複数あるProjectをうけ取れるようにする
    public ObservableCollection<ProjectResponse> Projects { get;} = new();

    // 複数あるMemberを受け取れるようにする
    public ObservableCollection<MemberResponse> Members { get; } = new();

    public ObservableCollection<TeamResponse> Teams {get;} = new();

    public MainWindowViewModel()
    {
        CurrentView = new TodoListView // アプリケーションの起動時にはTodoのviewを持つように指定する
        {
            DataContext = this // TodoListView の Binding 先を MainWindowViewModel にする
        };
        /*
         LoadTodosAsyncが非同期で取得するメソッドになっているから
         awaitって書きたいけど、コンストラクタではawaitを書けない。

         そのため、使わない変数 _ に入れる形で、
         画面が作られたタイミングでTodo取得処理を開始している。

         ここでの _ は「戻り値は使わないが、処理は開始したい」という意味で使っている。
         */
        _ = LoadTodosAsync();
        _ = LoadProjectsAsync();
        _ = LoadTeamsAsync();
        _ = LoadMembersAsync();
    }

    /*
     APIからTodo一覧を読み込む処理。

     async Taskになっているのは、HTTP通信のように時間がかかる処理を
     画面を止めずに待つため。

     流れ:
     1. APIへGETリクエストを送る
     2. JSONをList<TodoResponse>として受け取る
     3. 画面表示用のTodosを一度空にする
     4. TodoResponseをTodoItemViewModelに詰め替える
     5. Todosに追加して、ListBoxに表示させる
     */
    private async Task LoadTodosAsync()
    {
        try
        {
            /*
             http://localhost:5128/api/todos にGETリクエストを送る
              ↓
             返ってきたJSONを List<TodoResponse> に変換する
              ↓
             結果を todos 変数に入れる
            */
            var todos = await _httpClient.GetFromJsonAsync<List<TodoResponse>>(
                "http://localhost:5128/api/todos"
            );

            allTodos.Clear();

            if (todos is null)
            {
                Todos.Clear();
                // APIからデータが返ってこなかった場合は、ここで処理を終える。
                return;
            }
            
            foreach (var todo in todos)
            {
                /*
                 APIから受け取ったTodoResponseを、
                 画面表示用のTodoItemViewModelに変換してTodosへ追加する。

                 APIの形と画面の形を分けておくと、
                 API側のデータ構造と画面表示の都合を切り離して考えやすくなる。
                 */
                allTodos.Add(new TodoItemViewModel
                {
                    Id = todo.Id,
                    Title = todo.Title,
                    Status = todo.Status,
                    ProjectName = todo.ProjectName ?? "Project未設定",
                    TeamName = todo.TeamName ?? $"TeamId: {todo.TeamId}",
                    MemberNames = todo.Members.Count == 0 ? "担当なし" : string.Join(", ", todo.Members.Select(m => m.MemberName))
                });
            }
            ApplyTodoFilter();
        }
        catch (Exception ex)
        {
            /*
             APIに接続できない、JSONの形が合わないなどの問題が起きた場合にここへ来る。

             今は学習用としてConsole.WriteLineでエラーを確認している。
             実際のアプリでは、画面にエラーメッセージを表示することも多い。
             */
            Console.WriteLine(ex);
        }
    }


    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects =await _httpClient.GetFromJsonAsync<List<ProjectResponse>>(
                "http://localhost:5128/api/Projects"
            );

            if (projects is null)
            {
                // APIからデータが返ってこなかった場合は、ここで処理を終える。
                return;
            }
            Projects.Clear();
            foreach(var project in projects)
            {
                Projects.Add(project);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Project一覧の取得に失敗しました: {ex.Message}");
        }
    }

    private async Task LoadTeamsAsync()
    {
        try
        {
            var teams = await _httpClient.GetFromJsonAsync<List<TeamResponse>>(
                "http://localhost:5128/api/teams"
            );

            Teams.Clear();

            if (teams is null)
            {
                return;
            }

            foreach (var team in teams)
            {
                Teams.Add(team);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Team一覧の取得に失敗しました: {ex.Message}");
        }
}

    private async Task LoadMembersAsync()
    {
        try
        {
            var members =await _httpClient.GetFromJsonAsync<List<MemberResponse>>(
                "http://localhost:5128/api/Members"
            );

            if (members is null)
            {
                // APIからデータが返ってこなかった場合は、ここで処理を終える。
                return;
            }
            Members.Clear();
            foreach(var member in members)
            {
                Members.Add(member);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Member一覧の取得に失敗しました: {ex.Message}");
        }
    }

    /*
    privateフィールドは小文字
    publicプロパティは大文字
    [ObservableProperty] が大文字プロパティを自動生成する
    XAML Bindingは大文字プロパティを見る
    
    */
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

    // 絞り込み用プロパティ
    [ObservableProperty]
    private string filterProjectName = string.Empty;

    [ObservableProperty]
    private string filterTeamName = string.Empty;


    // CommunityToolkit.Mvvmの機能で、AddTodoCommandを作ってくれて、xaml側でBindingコマンドとして使用できるようになる
    [RelayCommand]
    private async Task AddTodoAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTodoTitle))
        {
            Console.WriteLine("タイトルを入力してください。");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTodoTeamName))
        {
            Console.WriteLine("Team名を入力してください。");
            return;
        }

        var team = Teams.FirstOrDefault(t => t.Name == NewTodoTeamName);

        if (team is null)
        {
            Console.WriteLine($"指定されたTeamが見つかりません: {NewTodoTeamName}");
            return;
        }

        var teamId = team.Id;
        
        var memberIds = new List<int>();

        if(!string.IsNullOrWhiteSpace(NewTodoMemberNames))
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
                    Console.WriteLine($"指定されたMemberが見つかりません: {memberName}");
                    return;
                }

                memberIds.Add(member.Id);
            }
        }
        Console.WriteLine($"memberIds: {string.Join(", ", memberIds)}");
        var request = new CreateTodoRequest
        {
            TeamId = teamId,
            Title = NewTodoTitle,
            Status = "Todo",
            MemberIds = memberIds
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5128/api/todos",
            request
        );

        if(!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Todo追加に失敗しました: {response.StatusCode}");
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"ErrorBody: {error}");
            return;
        }
        NewTodoTitle = string.Empty;
        NewTodoTeamName = string.Empty;
        NewTodoMemberNames = string.Empty;

        await LoadTodosAsync();
    }

    [RelayCommand]
    private void ShowTodoList()
    {
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
        if(string.IsNullOrWhiteSpace(NewProjectName))
        {
            Console.WriteLine("Project名を入力してください");
            return;
        }

        var request = new CreateProjectRequest
        {
            Name = NewProjectName
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5128/api/Projects",
            request
        );
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Project追加に失敗しました");
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"ErrorBody: {error}");
            return;
        }

        NewProjectName = string.Empty;
        await LoadProjectsAsync();
        Console.WriteLine("Projectを追加しました");
    }

    [RelayCommand]
    private async Task AddTeamAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTeamProjectName))
        {
            Console.WriteLine("Project名を入力してください。");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTeamName))
        {
            Console.WriteLine("Team名を入力してください。");
            return;
        }

        var project = Projects.FirstOrDefault(p => p.Name == NewTeamProjectName);

        if (project is null)
        {
            Console.WriteLine($"指定されたProjectが見つかりません: {NewTeamProjectName}");
            return;
        }

        var request = new CreateTeamRequest
        {
            ProjectId = project.Id,
            Name = NewTeamName
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5128/api/teams",
            request
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Team追加に失敗しました");
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"ErrorBody: {error}");
            return;
        }

        NewTeamProjectName = string.Empty;
        NewTeamName = string.Empty;
        LoadTeamsAsync();
        Console.WriteLine("Teamを追加しました");
    }

    [RelayCommand]
    private async Task AddMemberAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMemberName))
        {
            Console.WriteLine("Member名を入力してください。");
            return;
        }

        var request = new CreateMemberRequest
        {
            Name = NewMemberName
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5128/api/members",
            request
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Member追加に失敗しました");
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"ErrorBody: {error}");
            return;
        }

        NewMemberName = string.Empty;
        await LoadMembersAsync();
        Console.WriteLine("Memberを追加しました");
    }

    [RelayCommand]
    private async Task DeleteTodoAsync(int todoId)
    {
        var response = await _httpClient.DeleteAsync(
            $"http://localhost:5128/api/todos/{todoId}"
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Todo削除に失敗しました");
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"ErrorBody: {error}");
            return;
        }

        await LoadTodosAsync();
    }

    [RelayCommand]
    private async Task UpdateTodoAsync(int todoId)
    {
        var request = new UpdateTodoStatusRequest
        {
            Status = "Done"
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"http://localhost:5128/api/todos/{todoId}/status",
            request
        );
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Status更新に失敗しました");
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"ErrorBody: {error}");
            return;
        }

        await LoadTodosAsync();
    }

    // 絞り込み処理
    private void ApplyTodoFilter()
    {
        var filteredTodos = allTodos.AsEnumerable();

        if(!string.IsNullOrWhiteSpace(FilterProjectName))
        {
            filteredTodos = filteredTodos.Where(todo =>
            todo.ProjectName.Contains(FilterProjectName,StringComparison.OrdinalIgnoreCase));
        }

        if(!string.IsNullOrWhiteSpace(FilterTeamName))
        {
            filteredTodos = filteredTodos.Where(todo =>
            todo.TeamName.Contains(FilterTeamName,StringComparison.OrdinalIgnoreCase));
        }

        Todos.Clear();

        foreach (var todo in filteredTodos)
        {
            Todos.Add(todo);
        }
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

}    
