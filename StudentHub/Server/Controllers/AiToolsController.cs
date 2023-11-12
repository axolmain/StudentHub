using StudentHub.Server.Services;
using StudentHub.Server.Services.AiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHub.Server.Controllers;

public class QuestionModel
{
    public string Question { get; set; }
}

[ApiController]
[Route("[controller]")]
public class AiToolsController : ControllerBase
{
    private readonly ChatAiService _chatAiService;
    private readonly ChatHistoryService _chatHistoryService;

    public AiToolsController(ChatAiService chatAiService, ChatHistoryService chatHistoryService)
    {
        _chatAiService = chatAiService;
        _chatHistoryService = chatHistoryService;
    }

    [HttpPost]
    [Route("chat")]
    public async Task<ActionResult<IEnumerable<ChatEntry>>> PostQuestion(string question, string studySessionId, string userGuid)
    {
        if (string.IsNullOrEmpty(question))
            return BadRequest("Question cannot be empty");
        string response = await _chatAiService.Execute(question, studySessionId);
        // return Ok(_chatHistoryService.GetMessages(studySessionId));
        return Ok(response);
    }
}