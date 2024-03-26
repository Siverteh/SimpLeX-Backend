// Including necessary namespaces for Identity, MVC, Data Context, and Models.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using SimpLeX_Backend.Services;

// Declaring the namespace for the controller, typically aligned with the project's folder structure.
namespace SimpLeX_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class
        AuthController : ControllerBase // Inherits from ControllerBase, providing many properties and methods for handling HTTP requests.
    {
        // Fields to hold the services injected through the constructor.
        private readonly UserManager<User> _userManager; // Manages users in a persistence store.
        private readonly ApplicationDbContext _db; // The EF Core database context.
        private readonly TokenService _tokenService; // A custom service for generating JWT tokens.

        // The constructor receives instances of UserManager, DbContext, and TokenService via dependency injection.
        public AuthController(UserManager<User> userManager, ApplicationDbContext db, TokenService tokenService)
        {
            _userManager = userManager;
            _db = db;
            _tokenService = tokenService;
        }

        // Marks this method as handling POST requests. It defines a specific route "register".
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register(RegisterViewModel request) 
        {
            // Checks if the model state is valid, i.e., if all required fields are present and valid.
            if (!ModelState.IsValid)
            {
                // If not valid, returns a 400 Bad Request with the model state errors.
                return BadRequest("ModelState is invalid!");
            }

            // Checks if the password and confirmation password match.
            if (request.Password != request.ConfirmPassword)
            {
                // If not, returns a 400 Bad Request.
                return BadRequest("Passwords do not match.");
            }
            
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // Directly return a BadRequest for email in use. Adjust the message as necessary.
                return BadRequest("Email is already in use.");
            }

            // Tries to create a new user with the provided username, email, and password.
            var result = await _userManager.CreateAsync(
                new User() { UserName = request.UserName, Email = request.Email },
                request.Password
            );

            // Checks if the user creation was successful.
            if (result.Succeeded)
            {
                // Clears the password for security before returning the response.
                request.Password = "";
                // Returns a 201 Created response, with a route to this register method, and the request object.
                return CreatedAtAction(nameof(Register), new { email = request.Email }, request);
            }

            // If the user creation failed, adds each error to the model state.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            // Returns a 400 Bad Request with all the accumulated errors.
            return BadRequest(ModelState);
        }
        
        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var managedUser = await _userManager.FindByNameAsync(request.UserName);
            if (managedUser == null)
            {
                return BadRequest("Bad credentials");
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password);
            if (!isPasswordValid)
            {
                return BadRequest("Bad credentials");
            }
            var userInDb = _db.Users.FirstOrDefault(u => u.UserName == request.UserName);
            if (userInDb is null)
                return BadRequest("User does not exist");
            
            var accessToken = _tokenService.CreateToken(userInDb);
            await _db.SaveChangesAsync();
            return Ok(new AuthResponse
            {
                UserName = userInDb.UserName,
                Email = userInDb.Email,
                Token = accessToken,
            });
        }
    }
}
