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

        var keyName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var contentType = file.ContentType;
        _logger.LogInformation($"File details: Name={keyName}, ContentType={contentType}");

        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = keyName,
                InputStream = file.OpenReadStream(),
                ContentType = contentType,
            };

            _logger.LogInformation($"Attempting to upload file to S3: Bucket={_bucketName}, Key={keyName}");
            var response = await _s3Client.PutObjectAsync(putRequest);
            _logger.LogInformation("File uploaded successfully to S3");

            var imageUrl = $"https://{_bucketName}.s3.amazonaws.com/{keyName}";

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
        catch (AmazonS3Exception awsEx)
        {
            _logger.LogError($"AWS S3 error: {awsEx.Message}", awsEx);
            return StatusCode(500, $"AWS S3 error: {awsEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Internal server error: {ex.Message}", ex);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}