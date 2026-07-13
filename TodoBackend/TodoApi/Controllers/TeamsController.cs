using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
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
        
        if (team is null)
        {
            return NotFound();
        }

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

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TeamResponse>> CreateTeam(CreateTeamRequest request)
    {
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);

        if (!projectExists)
        {
            return BadRequest($"ProjectId {request.ProjectId} does not exist.");
        }

        var teamNameExists = await _context.Teams
            .AnyAsync(t => t.ProjectId == request.ProjectId && t.Name == name);

        if (teamNameExists)
        {
            return Conflict($"Team name '{name}' already exists in ProjectId {request.ProjectId}.");
        }

        var team = new Team
        {
            ProjectId = request.ProjectId,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);

        await _context.SaveChangesAsync();

        var response = new TeamResponse
        {
            Id = team.Id,
            ProjectId = team.ProjectId,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };

        return CreatedAtAction(nameof(GetTeam), routeValues: new { id = team.Id }, response);
    }

    [HttpPost("{teamId}/members/{memberId}")]
    public async Task<ActionResult<TeamMemberResponse>> CreateTeamMember(int teamId, int memberId, CreateTeamMemberRequest request)
    {
        // 指定されたteamIdがチームテーブルに存在しているか
        var teamExists = await _context.Teams.AnyAsync(t => t.Id == teamId);
        // 指定されたmemberIdがメンバーテーブルに存在しているか
        var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);

        if (!teamExists)
        {
            return BadRequest($"TeamId {teamId} does not exist.");
        }

        if (!memberExists)
        {
            return BadRequest($"MemberId {memberId} does not exist.");
        }

        var teamMemberExists = await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId && tm.MemberId == memberId);

        if (teamMemberExists)
        {
            return Conflict($"MemberId {memberId} is already assigned to TeamId {teamId}.");
        }

        var teamMember = new TeamMember
        {
            TeamId = teamId,
            MemberId = memberId,
            Position = request.Position
        };

        _context.TeamMembers.Add(teamMember);

        await _context.SaveChangesAsync();

        var response = new TeamMemberResponse
        {
            Id = teamMember.Id,
            TeamId = teamMember.TeamId,
            MemberId = teamMember.MemberId,
            Position = teamMember.Position.ToString()
        };

        return Ok(response);
    }
}
