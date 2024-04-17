using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;  // Your context
using SimpLeX_Backend.Models; // Your models
using Amazon.S3;
using Amazon.S3.Model;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<ImagesController> _logger;


    public ImagesController(ApplicationDbContext context, IAmazonS3 s3Client, IConfiguration configuration, ILogger<ImagesController> logger)
    {
        _context = context;
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"];
        _logger = logger;
    }
    
    [HttpGet("GetImages")]
    public async Task<IActionResult> GetImages(string projectId)
    {
        if (string.IsNullOrEmpty(projectId))
        {
            return BadRequest("Project ID is required.");
        }

        try
        {
            var images = await _context.Images
                .Where(img => img.ProjectId == projectId)
                .Select(img => new { img.ImagePath, img.ImageId })
                .ToListAsync();

            return Ok(new { images = images });  // Wrap in an object with 'images' as a key
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get images", ex);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpPost("UploadImage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string projectId)
    {
        _logger.LogInformation("Received a request to upload an image.");

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload failed: No file uploaded.");
            return BadRequest("No file uploaded.");
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            _logger.LogWarning("Upload failed: Project ID is required.");
            return BadRequest("Project ID is required.");
        }

        var fileName = Guid.NewGuid().ToString() + file.FileName.ToString();
        var filePath = Path.Combine("/data/images", fileName);  // Ensure this path is correctly mapped to your PVC mount
        _logger.LogInformation($"File details: Name={fileName}, ContentType={file.ContentType}");

        try
        {
            // Save the file to the local file system
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            _logger.LogInformation("File uploaded successfully to local storage");

            var imageUrl = $"{fileName}";  // Adjust the URL path according to how you serve static content

            var image = new Image
            {
                ProjectId = projectId,
                ImagePath = imageUrl,
                CreationDate = DateTime.UtcNow
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Database updated successfully. Image ID: {image.ImageId}");

            return Ok(new { success = true, url = imageUrl, imageId = image.ImageId });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Internal server error: {ex.Message}", ex);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    
    [HttpDelete("DeleteImage")]
    public async Task<IActionResult> DeleteImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return BadRequest("Image path is required.");
        }

        // Validate the imagePath to avoid path traversal issues
        var fileName = Path.GetFileName(imagePath);
        var image = await _context.Images.FirstOrDefaultAsync(img => img.ImagePath.EndsWith(fileName));
        if (image == null)
        {
            return NotFound("Image not found.");
        }

        // Proceed to delete from database
        _context.Images.Remove(image);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Image deleted from database.");

        // Attempt to delete from file system
        var filePath = Path.Combine("/data/images", fileName);  // Construct path safely
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            _logger.LogInformation("Image file deleted from storage.");
            return Ok(new { success = true, message = "Image deleted successfully." });
        }
        else
        {
            _logger.LogWarning("Image file not found on storage.");
            return Ok(new { success = false, message = "Image file not found, but database entry removed." });
        }
    }

}