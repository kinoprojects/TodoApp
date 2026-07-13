using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;

namespace TodoApi.Tests;

/*
 APIをテストの中で起動するための専用Factory。

 Controllerを直接newしてテストする方法もあるが、このFactoryを使うと
 ルーティング、JSON変換、Controller、EF Coreまで含めてHTTP経由で確認できる。
 そのため「実際にAPIとして呼ばれたときに期待どおりか」を小さく確認しやすい。
*/
public sealed class TodoApiTestFactory : WebApplicationFactory<Program>
{
    /*
     SQLiteのin-memory DBを使う。
     MySQLコンテナを起動しなくてもテストできるので、学習中でも気軽に実行できる。

     注意点として、SQLiteのin-memory DBは接続が閉じると中身も消える。
     そのためFactoryが生きている間は同じconnectionを開いたままにしている。
    */
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        /*
         Program.cs側で「Testing環境なら本番用MySQLを登録しない」と分岐している。
         ここでTesting環境にしておくことで、テスト用DBへ安全に差し替えられる。
        */
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            /*
             Program.csには接続文字列チェックがある。
             テストでは実際にはSQLiteへ差し替えるが、設定値自体は用意しておく。
            */
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=todo_test;User=root;Password=test;"
            });
        });

        builder.ConfigureServices(services =>
        {
            /*
             もしAppDbContextの登録が残っていたら一度外す。
             EF Coreは1つのDbContextに複数のDB providerを同時登録できないため、
             本番用MySQLとテスト用SQLiteが混ざらないようにしている。
            */
            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            connection.Open();

            /*
             テストではSQLiteを使う。
             本番コードのControllerやAppDbContextはそのまま使い、DB接続先だけを差し替えるのがポイント。
            */
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(connection);
            });

            /*
             Migrationを実行する代わりに、今のModel定義からテストDBのテーブルを作る。
             テスト開始時に必要なテーブルをすぐ用意できる。
            */
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // Factoryの寿命が終わったら、テスト用DB接続も閉じる。
            connection.Dispose();
        }
    }
}
