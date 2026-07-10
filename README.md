## 実行環境

- .NET 10.0.x
- Docker / Docker Compose

## 起動手順

### 1. MySQLを起動
 
```bash
cd TodoBackend/TodoApi
docker compose up -d
```

### 2. DBを更新

```bash
dotnet ef database update
```

### 3. APIを起動

```bash
dotnet run --project TodoBackend/TodoApi/TodoApi.csproj
```

API は `http://localhost:5128` で起動します。


### 4. Avalonia アプリを起動

```bash
dotnet run --project TodoFrontend/TodoFrontend.csproj
```

### 5. APIの確認方法

```bash
curl http://localhost:5128/Todos
```
このAPIではタスクの一覧を取得します。

### 6. テストの実行方法

```bash
dotnet test
```