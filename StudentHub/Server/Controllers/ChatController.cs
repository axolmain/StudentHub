using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Services;
using StudentHub.Server.Services.AiServices;
using StudentHub.Server.Services.DataService;
using StudentHub.Shared;

namespace StudentHub.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ChatHistoryService _chatHistoryService;
    private readonly IDataService dataService;
    
    public ChatController(IChatService chatService, ChatHistoryService chatHistoryService, 
        IDataService dataService)
    {
        _chatService = chatService;
        _chatHistoryService = chatHistoryService;
        this.dataService = dataService;
    }

    [HttpGet("GetMessages")]
    public async Task<IEnumerable<ChatMessage>> GetMessages(string userId)
    {
        return await _chatService.GetMessagesForUser(userId);
    }

    [HttpPost("PostMessage")]
    public async Task<IActionResult> PostMessage([FromBody] ChatMessage message,string studySessionId, string userGuid)
    {
        if (string.IsNullOrEmpty(message.Message))
            return BadRequest("Question cannot be empty");
        
        await _chatService.AddMessage(message);
        
        studySessionId = await dataService.GetStudySessionId(studySessionId, userGuid);
        return Ok(_chatHistoryService.GetMessages(studySessionId));
    }
}
