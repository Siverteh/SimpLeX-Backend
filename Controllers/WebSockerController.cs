using Microsoft.AspNetCore.Http;
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
                    var userName = HttpContext.Request.Query["userName"].ToString();
                    if (string.IsNullOrEmpty(userName))
                    {
                        HttpContext.Response.StatusCode = 400; // Bad Request if no userName
                        await HttpContext.Response.WriteAsync("userName parameter is required");
                        return;
                    }

                    var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _webSocketService.AddSocketToProject(projectId, webSocket, userName); // Pass userName here
                    await _webSocketService.HandleWebSocketAsync(projectId, webSocket);
                }
                else
                {
                    HttpContext.Response.StatusCode = 400; // Bad request if projectId is missing
                    return;
                }
            }
            else
            {
                HttpContext.Response.StatusCode = 400; // Not a WebSocket request
            }
        }

    }
}