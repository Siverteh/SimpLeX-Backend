using System.ComponentModel.DataAnnotations;

namespace SimpLeX_Backend.Models
{
    public class RegisterViewModel
    {
        [Required] public string UserName { get; set; } = null!;
        [Required] public string Email { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;
        [Required] public string ConfirmPassword { get; set; } = null!;
    }
    public class AuthRequest
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponse
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}