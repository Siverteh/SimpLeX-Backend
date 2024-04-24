using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[Route("api/[controller]")]
[ApiController]
public class TemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ApplicationDbContext context, ILogger<TemplatesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Templates/GetTemplates
    [HttpGet("GetTemplates")]
    public async Task<IActionResult> GetTemplates()
    {
        try
        {
            var templates = await _context.Templates.ToListAsync();
            return Ok(new { success = true, templates });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve templates: {Exception}", ex);
            return StatusCode(500, "Internal server error while retrieving templates");
        }
    }

    // POST: api/Templates/AddTemplate
    [HttpPost("AddTemplate")]
    public async Task<IActionResult> AddTemplate([FromBody] Template template)
    {
        _logger.LogInformation($"Received template to add: {JsonConvert.SerializeObject(template)}");

        if (template == null)
        {
            _logger.LogError("Template object is null.");
            return BadRequest("Template object is required.");
        }

        try
        {
            template.CreatedDate = DateTime.UtcNow.AddHours(2);
            template.ModifiedDate = DateTime.UtcNow.AddHours(2);

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Template added successfully: {template.TemplateId}");
            return Ok(new { success = true, templateId = template.TemplateId, message = "Template added successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to add template: {Exception}", ex);
            return StatusCode(500, "Internal server error while adding template");
        }
    }

    // DELETE: api/Templates/DeleteTemplate/{templateId}
    [HttpDelete("DeleteTemplate/{templateId}")]
    public async Task<IActionResult> DeleteTemplate(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return BadRequest("Template ID is required.");
        }

        var template = await _context.Templates.FirstOrDefaultAsync(t => t.TemplateId == templateId);
        if (template == null)
        {
            return NotFound("Template not found.");
        }

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Template deleted successfully.");

        return Ok(new { success = true, message = "Template deleted successfully." });
    }
}
