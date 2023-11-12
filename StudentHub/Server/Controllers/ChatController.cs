using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Services;
using StudentHub.Shared;

namespace StudentHub.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("GetMessages")]
    public async Task<IEnumerable<ChatMessage>> GetMessages(string userId)
    {
        return await _chatService.GetMessagesForUser(userId);
    }

    [HttpPost("PostMessage")]
    public async Task<IActionResult> PostMessage([FromBody] ChatMessage message)
    {
        await _chatService.AddMessage(message);
        return Ok();
    }
}
