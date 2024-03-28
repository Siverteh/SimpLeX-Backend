using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;

namespace SimpLeX_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Projects
        [HttpGet]
        [Route("Projects")]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var userId = _userManager.GetUserId(User);
            var projects = await _context.Projects
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return projects;
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
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                UserId = userId // Set the user ID
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project created successfully.", projectId = project.ProjectId });
        }

    }
}