using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/*
 データベースを使うための窓口になるクラス。

 このクラスがあることで、EF Coreは
 「このアプリではTodoItemというデータをデータベースで扱う」
 ということを知ることができる。

 実際にどのデータベースへ接続するかは、このファイルではなく
 Program.csのAddDbContextで設定している。
*/
public class AppDbContext : DbContext
{
    /*
     AppDbContextが作られるときに呼ばれる部分。

     optionsには、データベースへ接続するための設定が入っている。
     たとえば「MySQLを使う」「接続先はどこか」といった情報。

     このoptionsはProgram.csで作られ、ここに渡される。

     流れのイメージ:
     1. Program.csで接続設定を作る
     2. AppDbContextにoptionsとして渡す
     3. base(options)で親のDbContextに渡す
     4. EF Coreがその設定を使ってデータベースを操作できるようになる
     */
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) // 受け取った設定を、親クラスであるDbContextにも渡す。
    {
    }

    /*
     TodoItemをデータベースのテーブルとして扱うための設定。

     このTodoItemsを通して、TodoItemの追加・取得・更新・削除ができる。
     基本的には、データベース側のTodoItemsテーブルに対応するものと考えるとわかりやすい。
     */
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<MemberTodoItem> MemberTodoItems => Set<MemberTodoItem>();

    /* 親クラスのDbContextに「OnModelCreating」というメソッドがあるので、そのメソッドを上書きするためにoverrideする。
       OnModelCreatingメソッドはMigration作成時にそれぞれのクラスからどのようなDB、テーブルにするかの情報を組み立てる
       引数「ModelBuilder modelBuilder」はEF coreに対してModelやプロパティに対して設定したい内容を伝えるオブジェクトとなっている。
    */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TodoItemのStatusカラムはDB上ではstringとして扱ってね、という設定
        modelBuilder.Entity<TodoItem>() // TodoItemモデルについて設定するよ
                    .Property(t => t.Status) // 設定したいのはTodoItemモデルのStatusだよ
                    .HasConversion<string>(); // DBに保存するときはstringにしてね。DBから戻すときはenumにしてね

        modelBuilder.Entity<TeamMember>()// TeamMemberモデルについて設定するよ
                    .Property(tm => tm.Position) // 設定したいのはTeamMemberモデルのPositionだよ
                    .HasConversion<string>(); // DBに保存するときはstringにしてね。DBから戻すときはenumにしてね

        modelBuilder.Entity<Project>()
                    .Property(p => p.Name)
                    .HasMaxLength(100); // 文字列の長さを決めてる

        modelBuilder.Entity<Team>()
                    .Property(t => t.Name)
                    .HasMaxLength(100);

        modelBuilder.Entity<Member>()
                    .Property(m => m.Name)
                    .HasMaxLength(100);

        modelBuilder.Entity<TodoItem>()
                    .Property(t => t.Title)
                    .HasMaxLength(200);

        /*
            Property　=> カラム単体の保存ルール
            HasIndex =>  検索・重複禁止に使う索引ルール
            と入った違いがある。
        */

        modelBuilder.Entity<Project>() // Projectモデルについて設定するよ
                    .HasIndex(p => p.Name) // 設定したいのはNameだよ
                    .IsUnique(); // ユニーク制約つけてね

        modelBuilder.Entity<Team>() // Teamモデルについて設定するよ
                    .HasIndex(t => new { t.ProjectId, t.Name }) // tを仮の入れ物として使って、その中のProjectIdとNameをセットで見てね。新しいカラムやテーブルを作っているわけではなく、複合ユニーク制約の対象としてまとめて扱いたいだけ。
                    .IsUnique(); // ユニーク制約つけてね

        modelBuilder.Entity<TeamMember>()
                    .HasIndex(tm => new { tm.TeamId, tm.MemberId })
                    .IsUnique();

        modelBuilder.Entity<MemberTodoItem>()
                    .HasIndex(mt => new { mt.MemberId, mt.TodoItemId })
                    .IsUnique();
    }
}
