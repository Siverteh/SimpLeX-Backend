using System.Threading.Tasks;

namespace SimpLeX_Backend.Services
{
    public interface IDocumentService
    {
        // Method to compile LaTeX code and return the PDF content as a byte array.
        Task<byte[]> CompileLatexAsync(string latexCode);
    }
}