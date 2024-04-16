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
    [Consumes("multipart/form-data")]  // Ensure the endpoint explicitly expects form-data
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string projectId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest("Project ID is required.");
        }

        var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        if (!Directory.Exists(uploadsFolderPath))
            Directory.CreateDirectory(uploadsFolderPath);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var image = new Image
        {
            ProjectId = projectId,
            ImagePath = filePath,
            CreationDate = DateTime.UtcNow  // If not defaulting in the model
        };

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var url = Url.Content($"~/uploads/{fileName}");  // Correctly generate the URL to access the file

        return Ok(new { success = true, url, imageId = image.ImageId });
    }
}