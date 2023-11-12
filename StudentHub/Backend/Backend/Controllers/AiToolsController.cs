using Backend.Services;
using Backend.Services.AiServices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

public class QuestionModel
{
    public string Question { get; set; }
}

[ApiController]
public class AiToolsController : ControllerBase
{
    private readonly ChatAiService _chatAiService;
    private readonly MaterialCacheService _materialCacheService;
    private readonly ChatHistoryService _chatHistoryService;

    public AiToolsController(ChatAiService chatAiService,
        MaterialCacheService materialCacheService,
        ChatHistoryService chatHistoryService)
    {
        _chatAiService = chatAiService;
        _materialCacheService = materialCacheService;
        _chatHistoryService = chatHistoryService;
    }

    [HttpPost]
    [Route("AiTools/chat")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ChatEntry>>> PostQuestion(QuestionModel questionModel, string studySessionId)
    {
        if (string.IsNullOrEmpty(questionModel?.Question))
            return BadRequest("Question cannot be empty");
        string response = await _chatAiService.Execute(questionModel.Question, studySessionId);
        return Ok(_chatHistoryService.GetMessages(studySessionId));
    }

    [HttpPost]
    [Route("AiTools/createFlashcards")]
    [Authorize]
    public async Task<IActionResult> CreateFlashcards(string? studySessionId)
    {
        return Ok(await _materialCacheService.GetFlashCards(studySessionId));
    }

    [HttpPost]
    [Route("AiTools/createMultipleChioce")]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoice(string? studySessionId)
    {
        return Ok(await _materialCacheService.GetQuiz(studySessionId));
    }
}