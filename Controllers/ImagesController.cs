using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;  // Your context
using SimpLeX_Backend.Models; // Your models

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ImagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("uploadImage")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string projectId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var imageBytes = new byte[file.Length];
        await file.OpenReadStream().ReadAsync(imageBytes, 0, imageBytes.Length);
        var imageBase64 = Convert.ToBase64String(imageBytes);

        var image = new Image
        {
            ProjectId = projectId,
            ImageData = imageBase64
        };

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, ImageId = image.ImageId });
    }

    [HttpGet("{projectId}")]
    public async Task<IActionResult> GetImages(string projectId)
    {
        var images = await _context.Images
            .Where(i => i.ProjectId == projectId)
            .Select(i => new { i.ImageId, i.ImageData })
            .ToListAsync();

        return Ok(images);
    }
}