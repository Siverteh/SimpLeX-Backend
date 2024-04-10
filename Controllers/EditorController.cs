using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using System.Linq;
using System.Security.Cryptography;
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

            // Ensure you load the User related to the Project and the Users related to the Collaborators.
            var project = await _context.Projects
                .Include(p => p.User) // Include the project owner details
                .Include(p => p.Collaborators) // Include details about collaborators
                .ThenInclude(c => c.User) // Include user details for each collaborator
                .FirstOrDefaultAsync(p => p.ProjectId == id && (p.UserId == userId || p.Collaborators.Any(c => c.UserId == userId)));

            if (project == null)
            {
                return NotFound("Project not found or access denied.");
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
        
        [HttpGet("Share/{projectId}")]
        public async Task<ActionResult> GenerateShareLink(string projectId)
        {
            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.UserId == userId);
            if (project == null)
            {
                return NotFound("Project not found or access denied.");
            }

            var token = GenerateSecureToken();
            var expiry = DateTime.UtcNow.AddDays(7); // Token validity for 7 days

            var shareLink = new ShareLink
            {
                ProjectId = projectId,
                InvitationToken = token,
                TokenExpiry = expiry
            };

            _context.ShareLinks.Add(shareLink);
            await _context.SaveChangesAsync();

            // Redirect to the frontend handling route
            var baseUrl = "http://10.225.149.19:31688";  // Change to your actual domain
            var link = $"{baseUrl}/Redeem/{token}";

            return Ok(new { link = link });
        }


        
        private string GenerateSecureToken()
        {
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[32];
                randomNumberGenerator.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }
        
        [HttpGet("RedeemInvitation/{token}")]
        public async Task<ActionResult> RedeemInvitation(string token)
        {
            var shareLink = await _context.ShareLinks
                .Include(l => l.Project)
                .SingleOrDefaultAsync(l => l.InvitationToken == token && l.TokenExpiry > DateTime.UtcNow);

            if (shareLink == null)
            {
                return NotFound("Invalid or expired token.");
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId)) {
                // If not logged in, just return the project ID to allow client-side handling
                return Ok(new { projectId = shareLink.ProjectId });
            }

            // Check if already a collaborator
            var existingCollaborator = await _context.Collaborators
                .AnyAsync(c => c.ProjectId == shareLink.ProjectId && c.UserId == userId);
            if (!existingCollaborator)
            {
                var collaborator = new Collaborator
                {
                    ProjectId = shareLink.ProjectId,
                    UserId = userId,
                    AccessType = "editor"  // Default access type
                };

                _context.Collaborators.Add(collaborator);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Access granted.", projectId = shareLink.ProjectId });
        }
    }
}
