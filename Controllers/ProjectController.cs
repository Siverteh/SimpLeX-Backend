using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using SimpLeX_Backend.Services;

namespace SimpLeX_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DocumentService _documentService;

        public ProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, DocumentService documentService)
        {
            _context = context;
            _userManager = userManager;
            _documentService = documentService;
        }

        // GET: api/Projects
        [HttpGet]
        [Route("Projects")]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var userId = _userManager.GetUserId(User);
            var projects = await _context.Projects
                .Where(p => p.UserId == userId || p.Collaborators.Any(c => c.UserId == userId))
                .Select(p => new
                {
                    ProjectId = p.ProjectId,
                    Title = p.Title,
                    Owner = p.Owner,
                    LastModifiedDate = p.LastModifiedDate,
                    IsCollaborator = p.Collaborators.Any(c => c.UserId == userId)
                })
                .ToListAsync();

            return Ok(projects);
        }



        
        [HttpPost("Create")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return BadRequest("Project title is required.");
            }

            // Extract user ID from the JWT token
            var userId = _userManager.GetUserId(User);
            var userName = _userManager.GetUserName(User);
            
            if (string.IsNullOrEmpty(userId) ||string.IsNullOrEmpty(userName))
            {
                return Unauthorized("User is not recognized.");
            }

            var project = new Project
            {
                Title = model.Title,
                Owner = userName,
                LatexCode = "", // Empty initially
                WorkspaceState = null,
                CreationDate = DateTime.UtcNow.AddHours(2),
                LastModifiedDate = DateTime.UtcNow.AddHours(2),
                UserId = userId // Set the user ID
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project created successfully.", projectId = project.ProjectId });
        }
        
        [HttpPost("CopyProject")]
        public async Task<IActionResult> CopyProject([FromBody] CopyProjectRequest model)
        {
            var originalProject = await _context.Projects.FindAsync(model.ProjectId);
            if (originalProject == null)
            {
                return NotFound("Original project not found.");
            }

            var userId = _userManager.GetUserId(User);
            if (originalProject.UserId != userId)
            {
                return Unauthorized("You do not have permission to copy this project.");
            }

            var newProject = new Project
            {
                Title = model.Title,
                LatexCode = originalProject.LatexCode,
                WorkspaceState = originalProject.WorkspaceState,
                Owner = originalProject.Owner, 
                CreationDate = DateTime.UtcNow.AddHours(2),
                LastModifiedDate = DateTime.UtcNow.AddHours(2),
                UserId = userId
            };

            _context.Projects.Add(newProject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project copied successfully.", projectId = newProject.ProjectId });
        }
        
        [HttpGet("ExportAsPDF/{projectId}")]
        public async Task<IActionResult> ExportAsPDF(string projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound("Project not found.");
            }

            // Optional: Check if the current user owns the project
            var userId = _userManager.GetUserId(User);
            if (project.UserId != userId)
            {
                return Unauthorized("You are not authorized to export this project.");
            }

            var compiledPdfContent = await _documentService.CompileLatexAsync(project.LatexCode);

            return File(compiledPdfContent, "application/pdf", $"{project.Title}.pdf");
        }

        
        // In ProjectController.cs
        [HttpGet("ExportAsTeX/{projectId}")]
        public async Task<IActionResult> ExportAsTeX(string projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            // Optional: Check if the current user owns the project
            var userId = _userManager.GetUserId(User);
            if (project.UserId != userId)
            {
                return Unauthorized("You are not authorized to export this project.");
            }

            var latexCode = project.LatexCode ?? "No content"; // Placeholder if null
            return File(Encoding.UTF8.GetBytes(latexCode), "application/x-tex", $"{project.Title}.tex");
        }
        
        // DELETE: api/Projects/Delete/{projectId}
        [HttpDelete("Delete/{projectId}")]
        public async Task<IActionResult> DeleteProject(string projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            // Optional: Check if the current user owns the project
            var userId = _userManager.GetUserId(User);
            if (project.UserId != userId)
            {
                return Unauthorized("You do not have permission to delete this project.");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project deleted successfully." });
        }
    }
}