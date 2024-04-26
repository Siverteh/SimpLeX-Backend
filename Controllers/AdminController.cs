using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;

namespace SimpLeX_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;


        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _logger = logger;
            _context = context;
        }

        [HttpPost("UploadImage")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            _logger.LogInformation("Received a request to upload an image.");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload failed: No file uploaded.");
                return BadRequest("No file uploaded.");
            }

            var fileName = file.FileName.ToString();
            var filePath = Path.Combine("/data/images", fileName); 
            _logger.LogInformation($"File details: Name={fileName}, ContentType={file.ContentType}");

            try
            {
                // Save the file to the local file system
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded successfully to local storage");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Internal server error: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [HttpPost("AddTemplate")]
        public async Task<IActionResult> AddTemplate([FromBody] TemplateRequest templateRequest)
        {
            _logger.LogInformation($"Received template to add: {templateRequest.TemplateName}");

            if (templateRequest == null)
            {
                _logger.LogError("Template object is null.");
                return BadRequest("Template object is required.");
            }

            var template = new Template
            {
                TemplateName = templateRequest.TemplateName,
                XMLContent = templateRequest.XMLContent,
                ImagePath = templateRequest.ImagePath,
                IsCustom = templateRequest.IsCustom,
                CreatedDate = DateTime.UtcNow.AddHours(2),
                ModifiedDate = DateTime.UtcNow.AddHours(2),
                UserId = null,
                User = null
            };
            try
            {
                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Template added successfully: {template.TemplateId}");
                return Ok(new { success = true, templateId = template.TemplateId, message = "Template added successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add template: {ex}");
                return StatusCode(500, $"Internal server error while adding template: {ex.Message}");
            }
        }
    }
}
