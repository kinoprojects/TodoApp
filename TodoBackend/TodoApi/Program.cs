using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using System.Text.Json.Serialization;

/*
 Program.csは、アプリを起動するときの準備を書く場所。

 他のファイルとのつながりをざっくり見ると、次のようになる。

 compose.yml
 → MySQLのコンテナを用意する

 環境変数 ConnectionStrings__DefaultConnection
 → MySQLへ接続するための文字列を渡す

 Program.cs
 → appsettings.jsonの接続文字列を読み取り、AppDbContextに渡す準備をする
 → Controllersフォルダのコントローラーを使えるようにする
 → HTTPリクエストをどの処理へ流すかを設定する

 AppDbContext.cs
 → Program.csから受け取った設定を使い、データベース操作の窓口になる

 TodosController.cs
 → Program.csで使えるようにしたAppDbContextを受け取り、
   Todoの取得・追加・更新・削除を行う

 WebAPIで迷ったときは、
 「起動時の設定はProgram.cs」
 「DBとのつなぎ役はAppDbContext」
 「URLごとの処理はController」
 と考えると立ち返りやすい。
 */

// Webアプリを作るための準備を始める。
// builderには、設定ファイルの読み取りや、使う機能の登録をする力がある。
var builder = WebApplication.CreateBuilder(args);

/*
 ControllersフォルダにあるControllerを使えるようにする。

 これがあることで、TodosController.csのようなControllerが
 HTTPリクエストを受け取れるようになる。
 */
builder.Services.AddControllers()
    //ControllerがJSONを読み書きするときの設定を追加して、enumをJSONでは文字列として扱うようにしている
    // allowIntegerValues: false にすることで、0や1のような数値enumの入力は受け付けない
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });

/*
 AppDbContextをアプリに登録する。

 登録しておくと、TodosControllerのコンストラクタに
 AppDbContext context と書くだけで、ASP.NET Coreが自動で渡してくれる。

 データベース接続の流れ:
 1. compose.ymlでMySQLを起動する
 2. 環境変数ConnectionStrings__DefaultConnectionに接続情報を書く
 3. Program.csでDefaultConnectionを読み取る
 4. UseMySQLに渡す
 5. AppDbContextがその設定でデータベースを操作できるようになる
 */
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        /*
         appsettings.jsonまたは環境変数からDefaultConnectionという名前の接続文字列を取ってくる。
         実際のユーザー名やパスワードはConnectionStrings__DefaultConnectionで渡す。
         */
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Set ConnectionStrings__DefaultConnection.");
        }

        // EF Coreに「MySQLを使って、この接続文字列でつないでください」と教える。
        options.UseMySQL(connectionString);
    });
}

/*
 OpenAPIを使えるようにする。

 OpenAPIは、APIのURLや使い方を確認するための説明書のようなもの。
 開発中に「どんなAPIがあるか」を確認しやすくなる。
 */
builder.Services.AddOpenApi();

// ここまで登録した設定をもとに、実際に動かすWebアプリを作る。
var app = builder.Build();

/*
 開発中だけOpenAPIを表示できるようにする。

 本番環境では公開したくない情報が含まれることもあるため、
 Developmentのときだけ有効にしている。
 */
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// httpで来たリクエストをhttpsへ案内する。
// 通信をより安全にするための設定。
app.UseHttpsRedirection();

/*
 Controllerに書いたURLと処理を使えるようにする。

 たとえばTodosController.csでは、
 [Route("api/[controller]")] と [HttpGet] などを使っている。
 このMapControllersがあることで、/api/todos へのアクセスが
 TodosControllerのメソッドにつながる。
 */
app.MapControllers();

// アプリを起動し、HTTPリクエストを待ち受ける。
app.Run();

public partial class Program
{
}
