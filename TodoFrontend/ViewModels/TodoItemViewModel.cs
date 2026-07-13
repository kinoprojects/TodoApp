using CommunityToolkit.Mvvm.ComponentModel;

namespace TodoFrontend.ViewModels;

/*
 Todo 1件分を画面表示用に整えたViewModel。
 ViewModelBaseを継承して[ObservableProperty]を使うことで、
 Statusなどの値が後から変わったときにもUIへ変更通知できる。
*/
public partial class TodoItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string projectName = string.Empty;

    [ObservableProperty]
    private string teamName = string.Empty;

    [ObservableProperty]
    private string memberNames = string.Empty;
}
