using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController: ControllerBase
{
    private readonly AppDbContext _context;


    public ProjectsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Project>>> GetProjects()
    {
        var projects = await _context.Projects
            .OrderBy(project => project.CreatedAt)
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await _context.Projects.Include(p => p.Teams)
                                             .ThenInclude(t => t.TeamMembers)
                                             .ThenInclude(tm => tm.Member)
                                             .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
        {
            return NotFound();
        }
        return Ok(project);
    }

     /*
     プロジェクトの追加

     疎通確認コマンド
     curl -X POST http://localhost:5128/api/Projects \ -H "Content-Type: application/json" \ -d '{"Name":"Aプロジェクト"}'
     */
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(Project project)
    {
        project.Id = 0;

        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        _context.Projects.Add(project);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }
}

