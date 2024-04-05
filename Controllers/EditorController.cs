using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using System.Linq;
using System.Threading.Tasks;
using SimpLeX_Backend.Services;

namespace SimpLeX_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class EditorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DocumentService _documentService;
        private readonly ILogger<EditorController> _logger;

        public EditorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, DocumentService documentService, ILogger<EditorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _documentService = documentService;
            _logger = logger;
        }

        // GET: api/Editor/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(string id)
        {
            var userId = _userManager.GetUserId(User);
            
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.UserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        [HttpPost("Compile")]
        public async Task<IActionResult> Compile([FromBody] LatexRequest model)
        {
            
            if (string.IsNullOrEmpty(model.ProjectId) || string.IsNullOrWhiteSpace(model.LatexCode))
            {
                return BadRequest("Invalid model.");
            }

            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
            {
                return NotFound("Project not found.");
            }
            
            var compiledPdfContent = await _documentService.CompileLatexAsync(model.LatexCode);
            
            if (compiledPdfContent.Length > 0)
            {
                return File(compiledPdfContent, "application/pdf", $"{project.Title.Replace(" ", "_")}_compiled.pdf");
            }
            else
            {
                return StatusCode(500, "Compilation failed.");
            }
        }
        
        [HttpPost]
        [Route("SaveLatex")]
        public async Task<IActionResult> SaveLatex([FromBody] LatexRequest model)
        {
            if (string.IsNullOrEmpty(model.ProjectId) || string.IsNullOrWhiteSpace(model.LatexCode))
            {
                return BadRequest("Invalid request data.");
            }

            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
            {
                return NotFound("Project not found.");
            }

            project.LatexCode = model.LatexCode;
            project.WorkspaceState = model.WorkspaceState;
            project.LastModifiedDate = DateTime.UtcNow.AddHours(2); // Update the last modified date
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
