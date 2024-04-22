using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[Route("api/[controller]")]
[ApiController]
public class CitationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CitationsController> _logger;

    public CitationsController(ApplicationDbContext context, ILogger<CitationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Citation/GetCitations
    [HttpGet("GetCitations")]
    public async Task<IActionResult> GetCitations(string projectId)
    {
        if (string.IsNullOrEmpty(projectId))
        {
            return BadRequest("Project ID is required.");
        }

        try
        {
            var citations = await _context.Citations
                .Where(c => c.ProjectId == projectId)
                .ToListAsync();

            return Ok(new { success = true, citations = citations });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve citations: {Exception}", ex);
            return StatusCode(500, "Internal server error while retrieving citations");
        }
    }

    [HttpPost("AddCitation")]
    public async Task<IActionResult> AddCitation([FromBody] Citation citation)
    {
        _logger.LogInformation($"Received citation to add: {JsonConvert.SerializeObject(citation)}");

        if (citation == null)
        {
            _logger.LogError("Citation object is null.");
            return BadRequest("Citation object is required.");
        }

        if (string.IsNullOrWhiteSpace(citation.ProjectId))
        {
            _logger.LogError("Project ID is not provided.");
            return BadRequest("Project ID is required.");
        }

        try
        {
            // Generate Citation Key
            string baseKey = citation.Authors?.Split(',')[0].Split(' ').LastOrDefault() ?? "UnknownAuthor";
            baseKey += citation.Year ?? "UnknownYear";
            string citationKey = baseKey;
            int suffix = 1;

            // Check for existing citations with the same base key
            var existingCitations = _context.Citations
                .Where(c => c.ProjectId == citation.ProjectId &&
                            c.Authors == citation.Authors &&
                            c.Year == citation.Year)
                .ToList();

            while (existingCitations.Any(c => c.CitationKey == citationKey))
            {
                citationKey = $"{baseKey}_{suffix++}";
            }

            // Set the generated citation key
            citation.CitationKey = citationKey;

            citation.CreatedDate = DateTime.UtcNow;
            citation.LastModifiedDate = DateTime.UtcNow;

            _context.Citations.Add(citation);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Citation added successfully: {citation.CitationId}");
            return Ok(new
            {
                success = true, citationId = citation.CitationId, citationKey = citation.CitationKey,
                message = "Citation added successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to add citation: {Exception}", ex);
            return StatusCode(500, "Internal server error while adding citation");
        }
    }


    // DELETE: api/Citation/DeleteCitation/{citationId}
    [HttpDelete("DeleteCitation")]
    public async Task<IActionResult> DeleteCitation(string citationId)
    {
        if (string.IsNullOrWhiteSpace(citationId))
        {
            return BadRequest("Citation ID is required.");
        }

        // Find the citation in the database using the provided ID
        var citation = await _context.Citations.FirstOrDefaultAsync(c => c.CitationId == citationId);
        if (citation == null)
        {
            return NotFound("Citation not found.");
        }

        // Proceed to delete the citation from the database
        _context.Citations.Remove(citation);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Citation deleted from database.");

        // Return a success message (no file system operation needed for citations)
        return Ok(new { success = true, message = "Citation deleted successfully." });
    }
}