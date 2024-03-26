using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SimpLeX_Backend.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _compilerServiceBaseUrl;

        public DocumentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Retrieve the base URL of the compiler service from the application's configuration.
            // Ensure that "CompilerService:BaseUrl" is defined in your appsettings.json or through environment variables.
            _compilerServiceBaseUrl = configuration["CompilerService:BaseUrl"];
        }

        public async Task<byte[]> CompileLatexAsync(string latexCode)
        {
            // Prepare the request content
            var content = new StringContent(latexCode, Encoding.UTF8, "text/plain");
            
            // Send a POST request to the compile endpoint of the compiler service
            var response = await _httpClient.PostAsync($"{_compilerServiceBaseUrl}/compile", content);
            
            if (response.IsSuccessStatusCode)
            {
                // If the request is successful, read the PDF content as a byte array
                var pdfContent = await response.Content.ReadAsByteArrayAsync();
                return pdfContent;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to compile the LaTeX document. Status Code: {response.StatusCode}. Response: {errorContent}");
            }
        }
    }
}