using System.Text;
using StudentHub.Server.Services;
using StudentHub.Server.Services.AiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Services.DataService;

namespace StudentHub.Server.Controllers;

public class QuestionModel
{
    public string Question { get; set; }
}

[ApiController]
[Route("octolearnapi/[controller]")]
public class AiToolsController : ControllerBase
{
    private readonly ChatAiService chatAiService;
    private readonly ChatHistoryService chatHistoryService;
    private readonly IDataService dataService;

    public AiToolsController(ChatAiService chatAiService, ChatHistoryService chatHistoryService,
        IDataService dataService)
    {
        this.chatAiService = chatAiService;
        this.chatHistoryService = chatHistoryService;
        this.dataService = dataService;
    }

    [HttpPost]
    [Route("sendChat")]
    public async Task<IActionResult> PostQuestion(string studySessionId, string userGuid)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        string question = await reader.ReadToEndAsync();
        studySessionId = await dataService.GetStudySessionId(studySessionId, userGuid);
        
        string response = await chatAiService.Execute(question, studySessionId, userGuid);

        return Ok(response);
    }
}