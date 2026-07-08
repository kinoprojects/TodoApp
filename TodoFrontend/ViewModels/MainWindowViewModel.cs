using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TodoFrontend.Models;

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

    /*
     ObservableCollectionは、中身が追加・削除されたことを画面に通知できるListみたいなもの。

     MainWindow.axamlのListBoxはこのTodosを見ている。
     そのため、TodosにTodoItemViewModelを追加すると、画面側にも反映される。

     TodoItemViewModelを複数持てる一覧を空で用意している。
     */
    public ObservableCollection<TodoItemViewModel> Todos { get; } = new();

    public MainWindowViewModel()
    {
        /*
         LoadTodosAsyncが非同期で取得するメソッドになっているから
         awaitって書きたいけど、コンストラクタではawaitを書けない。

         そのため、使わない変数 _ に入れる形で、
         画面が作られたタイミングでTodo取得処理を開始している。

         ここでの _ は「戻り値は使わないが、処理は開始したい」という意味で使っている。
         */
        _ = LoadTodosAsync();
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

            if (todos is null)
            {
                // APIからデータが返ってこなかった場合は、ここで処理を終える。
                return;
            }

            /*
             すでに画面に表示されているTodoを一度消す。

             これをしないで追加だけすると、再読み込みしたときに
             同じTodoが重複して表示される可能性がある。
             */
            Todos.Clear();

            foreach (var todo in todos)
            {
                /*
                 APIから受け取ったTodoResponseを、
                 画面表示用のTodoItemViewModelに変換してTodosへ追加する。

                 APIの形と画面の形を分けておくと、
                 API側のデータ構造と画面表示の都合を切り離して考えやすくなる。
                 */
                Todos.Add(new TodoItemViewModel
                {
                    Id = todo.Id,
                    Title = todo.Title,
                    Status = todo.Status,
                    TeamName = $"TeamId: {todo.TeamId}",
                    MemberNames = todo.Members.Count == 0 ? "担当なし" : string.Join(", ", todo.Members.Select(m => m.MemberName))
                });
            }
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

}    
