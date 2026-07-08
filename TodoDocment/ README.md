# TodoApi

ASP.NET Core + MySQL + EF Core を使った ToDo API です。

## 使用技術

* ASP.NET Core / .NET 10
* MySQL
* Docker Compose
* EF Core
* LINQ

---

## 初回セットアップ

### .NET のバージョン確認

```bash
dotnet --version
```

現在使われている .NET SDK のバージョンを確認します。

---

### プロジェクト作成

```bash
dotnet new webapi -n TodoApi
```

ASP.NET Core Web API プロジェクトを作成します。

```bash
cd TodoApi
```

作成したプロジェクトフォルダに移動します。

---

### EF Core 関連パッケージ追加

```bash
dotnet add package MySql.EntityFrameworkCore
```

EF Core から MySQL に接続するための Provider を追加します。

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Migration など、EF Core の開発用機能を使うためのパッケージを追加します。

---

### dotnet-ef のセットアップ

```bash
dotnet new tool-manifest
```

このプロジェクト用のローカルツール管理ファイルを作成します。

```bash
dotnet tool install dotnet-ef
```

EF Core の CLI コマンドを使えるようにします。

```bash
dotnet ef --version
```

`dotnet ef` が使えるか確認します。

---

## Docker / MySQL

### MySQL を起動する

```bash
docker compose up -d
```

`compose.yml` に定義された MySQL コンテナをバックグラウンドで起動します。

---

### コンテナの状態を確認する

```bash
docker compose ps
```

MySQL コンテナが起動しているか確認します。

---

### MySQL のログを確認する

```bash
docker compose logs mysql
```

MySQL コンテナのログを確認します。起動に失敗したときなどに使います。

---

### MySQL にログインする

```bash
docker exec -it todo-mysql mysql -u todo_user -p todo_db
```

Docker 上で動いている MySQL にログインします。

パスワード:

```txt
todo_password
```

---

### MySQL コンテナを停止・削除する

```bash
docker compose down
```

コンテナを停止して削除します。

通常は volume が残るため、DB データは消えません。

---

### DB データも含めて削除する

```bash
docker compose down -v
```

コンテナと volume を削除します。

DB データも消えるため注意してください。

---

## EF Core / Migration

### Migration を作成する

```bash
dotnet ef migrations add InitialCreate
```

現在の Model / DbContext をもとに、DB変更用の Migration ファイルを作成します。

---

### Migration をDBに反映する

```bash
dotnet ef database update
```

作成済みの Migration を MySQL に反映します。

テーブル作成やカラム追加などが実行されます。

---

### 2回目以降のMigration例

```bash
dotnet ef migrations add AddTodoDueDate
dotnet ef database update
```

Model を変更した場合は、新しい名前で Migration を追加してからDBに反映します。

---

## アプリ起動

```bash
dotnet run
```

ASP.NET Core アプリを起動します。

起動後、ターミナルに表示されたURLを使ってAPIにアクセスします。

例:

```txt
http://localhost:5252
```

---

## API確認用 curl

以下の `5252` は、自分の環境で表示されたポート番号に置き換えてください。

---

### Todo一覧取得

```bash
curl http://localhost:5128/api/todos
```

Todo一覧を取得します。

---

### Todo作成

```bash
curl -X POST http://localhost:5128/api/todos -H "Content-Type: application/json" -d '{"TeamId":"1","title":"設計","stasus":"Todo"}'
```

新しいTodoを作成します。

---

### Todoを1件取得

```bash
curl http://localhost:5252/api/todos/1
```

IDが `1` のTodoを取得します。

---
å
### Todo更新

```bash
curl -X PUT http://localhost:5252/api/todos/2 -H "Content-Type: application/json" -d '{"title":"要件定義・設計","stasus":"InProgress"}'
```

IDが `1` のTodoを更新します。

---

### Todo削除

```bash
curl -X DELETE http://localhost:5252/api/todos/1
```

IDが `1` のTodoを削除します。

---

## よく使う開発時の流れ

### 通常起動

```bash
docker compose up -d
dotnet run
```

MySQLを起動してから、ASP.NET Coreアプリを起動します。

---

### DB構造を変更したとき

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

Model や DbContext を変更してDB構造が変わる場合に実行します。

---

### API確認

```bash
curl http://localhost:5252/api/todos
```

アプリ起動後、別ターミナルからAPIを確認します。

---

## 補足

* `dotnet run` を実行しているターミナルはアプリ起動用として使います。
* `curl` は別ターミナルから実行します。
* `docker compose down` では通常DBデータは残ります。
* `docker compose down -v` を実行するとDBデータも消えます。
* `SaveChangesAsync()` を呼ばないと、EF Coreの変更はDBに反映されません。






curl -X POST http://localhost:5128/api/teams/1/members/4 -H "Content-Type: application/json" -d '{"Position":1}'

An unhandled exception occurred while processing the request.

curl -X POST http://localhost:5128/api/todos/1/members/1 