using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController: ControllerBase
{
    private readonly AppDbContext _context;


    public MembersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Member>>> GetMembers()
    {
        var members = await _context.Members
            .OrderBy(member => member.CreatedAt)
            .ToListAsync();

        return Ok(members);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Member>> GetMember(int id)
    {
        // .includeは何個でもかける
        var member = await _context.Members.Include(m => m.TeamMembers)
                                                .ThenInclude(tm => tm.Team)
                                           .Include(m => m.MemberTodoItems)
                                                .ThenInclude(mt => mt.TodoItem)
                                           .FirstOrDefaultAsync(m => m.Id == id);


        if (member is null)
        {
            return NotFound();
        }

        var response = new
        {
            member.Id,
            member.Name,
            member.CreatedAt,
            member.UpdatedAt,
            teams = member.TeamMembers.Select(tm => new
            {
                tm.TeamId,
                TeamName = tm.Team == null ? null : tm.Team.Name,
                Position = tm.Position.ToString()
            }),
            Todos = member.MemberTodoItems.Select(mt => new
            {
                mt.Id,
                mt.TodoItem?.Title,
                Status = mt.TodoItem == null ? null : mt.TodoItem.Status.ToString(),
            })
        };
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<Member>> CreateMember(Member member)
    {
        member.Id = 0;

        member.CreatedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;

        _context.Members.Add(member);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
    }
}