using Microsoft.AspNetCore.Mvc;
using SimpLeX_Backend.Models; // Replace with the actual namespace of your models
using System.Threading.Tasks;
using SimpLeX_Backend.Services;

[Route("api/[controller]")]
[ApiController]
public class DocumentController : ControllerBase
{
    private readonly DocumentService _documentService; // Assuming you have a service for handling document operations

    public DocumentController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("Compile")]
    public async Task<IActionResult> Compile([FromBody] LatexDocument document)
    {
        if (document == null || string.IsNullOrWhiteSpace(document.LatexCode))
        {
            return BadRequest("Invalid LaTeX code.");
        }

        try
        {
            var compiledPdfContent = await _documentService.CompileLatexAsync(document.LatexCode);

            // Check if the PDF content is not null or empty
            if (compiledPdfContent != null && compiledPdfContent.Length > 0)
            {
                // Return the PDF content as a file
                return File(compiledPdfContent, "application/pdf", "compiledDocument.pdf");
            }
            else
            {
                return StatusCode(500, "Compilation failed.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception details
            return StatusCode(500, "An error occurred while compiling the document.");
        }
    }
}
