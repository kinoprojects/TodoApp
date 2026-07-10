using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Net.Http;
using TodoFrontend.Services;
using TodoFrontend.ViewModels;
using TodoFrontend.Views;

namespace TodoFrontend;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            /*
             HttpClientはここで作り、ViewModelには直接渡さない。
             ViewModelへ渡すのはTodoApiClientなので、ViewModelはHTTPの作り方を知らなくてよい。
            */
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(TodoApiOptions.BaseUrl)
            };
            var viewModel = new MainWindowViewModel(new TodoApiClient(httpClient));

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            /*
             初期ロードはWindowが開いてから始める。
             コンストラクタ内で通信しないことで、デザイン時表示やテストで勝手にAPIへ接続しないようにしている。
            */
            desktop.MainWindow.Opened += async (_, _) => await viewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
