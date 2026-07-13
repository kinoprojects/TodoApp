## 実行環境

- .NET 10.0.x
- Docker / Docker Compose

## 起動手順

### 1. 環境変数ファイルを作成

```bash
cd TodoBackend/TodoApi
cp .env.example .env
```

作成した `.env` の `MYSQL_PASSWORD`、`MYSQL_ROOT_PASSWORD`、`ConnectionStrings__DefaultConnection` を自分の環境に合わせて変更します。
`.env` は Git に含めず、共有用には `.env.example` を使います。

### 2. MySQLを起動
 
```bash
docker compose up -d
```

### 3. DBを更新

```bash
set -a
source .env
set +a
dotnet ef database update
```

### 4. APIを起動

```bash
set -a
source .env
set +a
dotnet run --project TodoApi.csproj
```

API は `http://localhost:5128` で起動します。


### 5. Avalonia アプリを起動

```bash
dotnet run --project ../../TodoFrontend/TodoFrontend.csproj
```

### 6. APIの確認方法

```bash
curl http://localhost:5128/Todos
```
このAPIではタスクの一覧を取得します。

### 7. テストの実行方法

```bash
dotnet test
```
