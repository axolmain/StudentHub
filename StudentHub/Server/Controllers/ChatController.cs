using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Services;
using StudentHub.Server.Services.AiServices;
using StudentHub.Shared;

namespace StudentHub.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ChatAiService _chatAiService;
    private readonly ChatHistoryService _chatHistoryService;
    
    public ChatController(IChatService chatService, ChatAiService chatAiService, ChatHistoryService chatHistoryService)
    {
        _chatService = chatService;
        _chatAiService = chatAiService;
        _chatHistoryService = chatHistoryService;
    }

    [HttpGet("GetMessages")]
    public async Task<IEnumerable<ChatMessage>> GetMessages(string userId)
    {
        return await _chatService.GetMessagesForUser(userId);
    }

    [HttpPost("PostMessage")]
    public async Task<IActionResult> PostMessage([FromBody] ChatMessage message, string studySessionId, string userGuid)
    {
        if (string.IsNullOrEmpty(message.Message))
            return BadRequest("Question cannot be empty");
        
        await _chatService.AddMessage(message);
        
        studySessionId = "SingleSession";
        string response = await _chatAiService.Execute(message.Message, studySessionId);
        return Ok(_chatHistoryService.GetMessages(studySessionId));
        return Ok();
    }
}
