using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
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
     // 引数でEF Entityを使うと不必要なデータまで受け取ってしまうことになる。そのためDtoを作成して受け取る情報を絞り込むようにする
     public async Task<ActionResult<ProjectResponse>> CreateProject(CreateProjectRequest request)
     {
         var name = request.Name.Trim();

         if (string.IsNullOrWhiteSpace(name))
         {
             return BadRequest("Name is required.");
         }

         var projectNameExists = await _context.Projects.AnyAsync(p => p.Name == name);

         if (projectNameExists)
         {
             return Conflict($"Project name '{name}' already exists.");
         }

         var project = new Project
         {
             Name = name,
             CreatedAt = DateTime.UtcNow,
             UpdatedAt = DateTime.UtcNow
         };

         _context.Projects.Add(project);
         await _context.SaveChangesAsync();

         var response = new ProjectResponse
         {
             Id = project.Id,
             Name = project.Name,
             CreatedAt = project.CreatedAt,
             UpdatedAt = project.UpdatedAt
         };

         return CreatedAtAction(nameof(GetProject), new { id = project.Id }, response);

     }
    
}
