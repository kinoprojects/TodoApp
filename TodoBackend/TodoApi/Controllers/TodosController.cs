using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

/*
 Todoに関するAPIの入り口になるクラス。

 このクラスの役割は大きく分けて3つ。
 1. フロントエンドやPostmanなどから届いたHTTPリクエストを受け取る
 2. AppDbContextを使ってデータベースのTodoItemsを操作する
 3. 処理の結果をHTTPレスポンスとして返す

 WebAPIを作るときは、まず次の流れを思い出すと理解しやすい。
 URLにアクセスされる
 → 対応するコントローラーのメソッドが呼ばれる
 → 必要ならデータベースを操作する
 → Ok、NotFound、CreatedAtAction、NoContentなどで結果を返す

 Program.csでAppDbContextを使えるように登録しているため、
 このコントローラーはコンストラクタでAppDbContextを受け取ることができる。
 */
namespace TodoApi.Controllers;

[ApiController] // WebAPI用のコントローラーであることをASP.NET Coreに教える。
[Route("api/[controller]")] // URLの基本形を決める。TodosControllerの場合は /api/todos でアクセスできる。
public class TodosController : ControllerBase // ControllerBaseは、WebAPIで返事を作るための基本機能を持つ親クラス。
{
    /*
     データベース操作に使うAppDbContextを、このクラスの中で使い回すための変数。

     readonlyは「コンストラクタで入れた後は別のものに入れ替えない」という意味。
     データベースへつなぐ窓口を途中で変えないようにしている。
     */
    private readonly AppDbContext _context;

    /*
     TodosControllerが作られるときに呼ばれる部分。

     Program.csで登録されたAppDbContextがここに渡される。
     それを_contextに入れておくことで、下のGet、Post、Put、Deleteの中で
     データベースを操作できるようになる。
     */
    public TodosController(AppDbContext context)
    {
        _context = context;
    }

    /*
     GET /api/todos

     Todoをすべて取得する処理。
     一覧画面を表示するときなどに使う。
     */
    [HttpGet]
    public async Task<ActionResult<List<TodoItem>>> GetTodos()
    {
        /*
         TodoItemsからデータを取得する。

         OrderByDescendingは「大きいものから順に並べる」という意味。
         ここではCreatedAtを使って、新しく作られたTodoが先に来るようにしている。

         ToListAsyncを呼んだタイミングで、実際にデータベースへ問い合わせる。
         */
        var todos = await _context.TodoItems
            .Include(t => t.MemberTodoItems)
            .ThenInclude(tm => tm.Member)
            .OrderBy(todo => todo.CreatedAt)
            .ToListAsync();
        
        // 複数あるtodosの中身を1つ1つ取り出して変換していっている。ここでのselectは選択ではないのがみそ。
        var responce = todos.Select(todo => new
        {
            todo.Id,
            todo.TeamId,
            todo.Title,
            Status = todo.Status.ToString(),
            todo.CreatedAt,
            todo.UpdatedAt,
            Members = todo.MemberTodoItems.Select(tm => new
            {
                tm.MemberId,
                MemberName = tm.Member?.Name
            })

        });

        return Ok(responce); // 200 OKとして、取得したTodo一覧をJSONで返す。
    }

    /*
     GET /api/todos/{id}

     指定されたidのTodoを1件だけ取得する処理。
     詳細画面を表示するときなどに使う。
     */
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetTodo(int id)
    {
        /*
         FindAsyncは、主キーを使ってデータを探す。
         TodoItemではIdが主キーなので、ここではidを指定して1件探している。
         awaitをつけることで検索が完了するまで次に進まない。なので、次でnullチェックができる。
         逆にawaitがついていないと検索中の処理がtodoに入るのでエラーとなる
         */
        var todo = await _context.TodoItems.Include(t => t.Team)
                                           .Include(t => t.MemberTodoItems)
                                           .ThenInclude(mt => mt.Member)
                                           .FirstOrDefaultAsync(t => t.Id == id);

        if (todo is null)
        {
            return NotFound(); // 見つからなかった場合は404 Not Foundを返す。
        }

        return Ok(todo); // 見つかった場合は200 OKとして、そのTodoをJSONで返す。
    }

    /*
     POST /api/todos

     新しいTodoを作成する処理。
     入力フォームから新しいTodoを追加するときなどに使う。
     */
    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateTodo(TodoItem todo)
    {
        /*
         Idを0にしておくと、EF Coreが「これは新しく追加するデータ」と判断し、
         データベースに保存するときに新しいIdを付けてくれる。
         */
        todo.Id = 0;

        // 作成日時はサーバー側で入れる。ユーザーから送られた日時をそのまま信じないため。
        todo.CreatedAt = DateTime.UtcNow;
        todo.UpdatedAt = DateTime.UtcNow;

        // Addは「追加予定」にする処理。まだこの時点ではデータベースには保存されていない。
        _context.TodoItems.Add(todo);

        // SaveChangesAsyncで、追加予定だった内容を実際にデータベースへ保存する。
        await _context.SaveChangesAsync();

        /*
         201 Createdを返す。

         CreatedAtActionは、
         「作成できました。作成したデータはGetTodoにこのidを渡すと取得できます」
         という意味をレスポンスに含められる。
         */
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, todo);
    }

    /*
     PUT /api/todos/{id}

     指定されたidのTodoを更新する処理。
     タイトルを変更したり、完了状態を切り替えたりするときに使う。
     */
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, TodoItem updatedTodo)
    {
        // まず、更新したいTodoが本当に存在するかを探す。
        var todo = await _context.TodoItems.FindAsync(id);

        if (todo is null)
        {
            return NotFound(); // 存在しないidなら404 Not Foundを返す。
        }

        /*
         変更してよい項目だけを、リクエストの内容で上書きする。

         IdやCreatedAtはここでは変更しない。
         「どの値をユーザーに変更させるか」を意識すると、安全なAPIを作りやすい。
         */
        todo.Title = updatedTodo.Title;

        todo.Status = updatedTodo.Status;

        todo.UpdatedAt = DateTime.UtcNow;

        // 上で書き換えた内容をデータベースへ保存する。
        await _context.SaveChangesAsync();

        return NoContent(); // 204 No Content。成功したが、返すデータはないという意味。
    }

    /*
     DELETE /api/todos/{id}

     指定されたidのTodoを削除する処理。
     */
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        // 削除したいTodoが存在するかを探す。
        var todo = await _context.TodoItems.FindAsync(id);

        if (todo is null)
        {
            return NotFound(); // 存在しないidなら404 Not Foundを返す。
        }

        // Removeは「削除予定」にする処理。まだこの時点ではデータベースから消えていない。
        _context.TodoItems.Remove(todo);

        // SaveChangesAsyncで、削除予定だった内容を実際にデータベースへ反映する。
        await _context.SaveChangesAsync();

        return NoContent(); // 204 No Content。削除は成功したが、返すデータはないという意味。
    }

    [HttpPost("{todoId}/members/{memberId}")]
    public async Task<IActionResult> AssignMemberToTodo(int todoId, int memberId)
    {
        var todoExists = await _context.TodoItems.AnyAsync(t => t.Id == todoId);
        var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);

        if (!todoExists || !memberExists)
        {
            return NotFound();
        }

        var memberTodoItem = new MemberTodoItem
        {
            TodoItemId = todoId,
            MemberId = memberId
        };

        _context.MemberTodoItems.Add(memberTodoItem);

        await _context.SaveChangesAsync();

        return Ok(memberTodoItem);
    }
}
