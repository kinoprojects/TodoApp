using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _context;
    public TeamsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Team>>> GetTeams()
    {
        var teams = await _context.Teams
            .OrderBy(team => team.CreatedAt)
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Team>> GetTeam(int id)
    {
        /*
         teamsテーブルを検索対象として、「.include」でそのteamsテーブルに紐づくTeamMemberも取得してね。というのが一行目
         そこに追加でincludeしてきたTeamsMemberの先にあるTeamsMemberに紐づくMemberも取得してね。これが二行目
         そして、そもそもteamsテーブルで検索するキーはidで最初に見つけた一行を取得してね。これが三行目
        */
        var team = await _context.Teams.Include(t => t.TodoItems)
                                            .ThenInclude(todo => todo.MemberTodoItems)
                                                .ThenInclude(mt => mt.Member)
                                       .Include(t => t.TeamMembers)
                                            .ThenInclude(tm => tm.Member)
                                       .FirstOrDefaultAsync(t => t.Id == id);

        var response = new
        {
            team.Id,
            team.Name,
            team.CreatedAt,
            team.UpdatedAt,
            Members = team.TeamMembers.Select(tm => new
            {
                tm.MemberId,
                MemberName = tm.Member?.Name,
                Position = tm.Position.ToString()
            }),
            TodoItem = team.TodoItems.Select(ti => new
            {
                ti.Id,
                ti.Title,
                Status = ti.Status.ToString(),
                ti.CreatedAt,
                ti.UpdatedAt,
                Members = ti.MemberTodoItems.Select(todomember => new
                {
                    todomember.Id,
                    MemberName = todomember.Member?.Name
                })
            })
        };
        
        if (team is null)
        {
            return NotFound();
        }



        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<Team>> CreateTeam(Team team)
    {
        team.Id = 0;

        team.CreatedAt = DateTime.UtcNow;
        team.UpdatedAt = DateTime.UtcNow;

        _context.Teams.Add(team);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeam), routeValues: new { id = team.Id }, team);
    }

    [HttpPost("{teamId}/members/{memberId}")]
    public async Task<ActionResult> CreateTeamMember(int teamId, int memberId, TeamMember teamMember)
    {
        // 指定されたteamIdがチームテーブルに存在しているか
        var teamExists = await _context.Teams.AnyAsync(t => t.Id == teamId);
        // 指定されたmemberIdがメンバーテーブルに存在しているか
        var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);

        // どちらかのIDが存在していない場合、notFoundで返す
        if (!teamExists || !memberExists)
        {
            return NotFound();
        }

        teamMember.Id = 0;
        teamMember.TeamId = teamId;
        teamMember.MemberId = memberId;

        _context.TeamMembers.Add(teamMember);

        await _context.SaveChangesAsync();

        return Ok();
    }
}