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
    private readonly ChatHistoryService _chatHistoryService;
    private readonly IChatService _chatService;
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
    
    [HttpPost("SaveChatSession")]
    public async Task<IActionResult> SaveChatSession([FromBody] List<ChatMessage> messages, string sessionId, string userId)
    {
        try
        {
            // Here, you should implement logic to save these messages to Cosmos DB.
            // For example, using the DataService you've shown.
            await dataService.SaveChatSession(messages, sessionId, userId);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }
    }
    
    [HttpGet("GetSessionMessages")]
    public async Task<ActionResult<IEnumerable<ChatMessage>>> GetSessionMessages(string sessionId, string userId)
    {
        try
        {
            var studySession = await dataService.GetStudySession(userId, sessionId);
            var messages = studySession.Messages;
            if (messages == null)
            {
                return NotFound("Session not found");
            }
            return Ok(messages);
        }
        catch (Exception ex)
        {
            // Log the exception here
            return StatusCode(500, $"Internal server error: {ex}");
        }
    }


    [HttpPost("PostMessage")]
    public async Task<IActionResult> PostMessage([FromBody] ChatMessage message, string studySessionId, string userGuid)
    {
        if (string.IsNullOrEmpty(message.Message))
            return BadRequest("Question cannot be empty");

        await _chatService.AddMessage(message);

        var studySession = await dataService.GetStudySession(studySessionId, userGuid);
        studySessionId = studySession.id;
        return Ok(_chatHistoryService.GetMessages(studySessionId));
    }
}