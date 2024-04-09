using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using SimpLeX_Backend.Services; // Adjust the namespace accordingly

namespace SimpLeX_Backend.Controllers
{
    public class WebSocketController : Controller
    {
        private readonly WebSocketService _webSocketService;

        public WebSocketController(WebSocketService webSocketService)
        {
            _webSocketService = webSocketService;
        }

        [HttpGet("/ws/{projectId}")]
        public async Task Get(string projectId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                
                if (!string.IsNullOrEmpty(projectId))
                {
                    var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _webSocketService.AddSocketToProject(projectId, webSocket); // Add the socket to the project group
                    await _webSocketService.HandleWebSocketAsync(projectId, webSocket);
                }
                else
                {
                    HttpContext.Response.StatusCode = 400;
                    return;
                }
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

    }
}