namespace TodoFrontend.Services;

/*
 APIの接続先を1か所にまとめておく。
 URLをViewModelや各メソッドに散らすと、ポート変更や本番環境への切り替えで修正漏れが起きやすい。
*/
public static class TodoApiOptions
{
    public const string BaseUrl = "http://localhost:5128/";
}
