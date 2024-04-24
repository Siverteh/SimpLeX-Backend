using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;

namespace SimpLeX_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{

    private readonly ApplicationDbContext _context;
    public ChatController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("SaveChatToDb")]
    public async Task SaveChatToDb(string projectId, dynamic chatData)
    {
        Console.WriteLine("Received chat data: UserId = {0}, Content = {1}, Timestamp = {2}, Username = {3}",
            chatData.userId, chatData.content, chatData.timestamp, chatData.username);

        if (_context == null)
        {
            Console.WriteLine("DbContext _context is null.");
        }

        if (_context.ChatMessages == null)
        {
            Console.WriteLine("_context.ChatMessages is null.");
        }


        try
        {
            var chat = new ChatMessage
            {
                ProjectId = projectId,
                UserId = chatData.userId,
                Message = chatData.content,
                Timestamp = chatData.timestamp,
                UserName = chatData.userName
            };


            _context.ChatMessages.Add(chat);
            await _context.SaveChangesAsync();
            Console.WriteLine("Chat saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }

            throw; 
        }
    }
    
    [HttpGet("GetChatMessages/{projectId}")]
    public async Task<IActionResult> GetChatMessages(string projectId)
    {
        var messages = await _context.ChatMessages
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.Timestamp)
            .ToListAsync();

        if (!messages.Any())
        {
            return NotFound("No messages found for this project.");
        }

        return Ok(messages);
    }

}