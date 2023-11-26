using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Services.AiServices;
using StudentHub.Server.Services.DataService;
using StudentHub.Shared;

namespace StudentHub.Server.Controllers;

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
    [Route("sendChat/{studySessionId}/{userGuid}")]
    public async Task<IActionResult> PostQuestion([FromRoute] string studySessionId, [FromRoute] string userGuid, [FromBody] JsonElement userQuestion)
    {
        string response = await chatAiService.Execute(userQuestion.ToString(), studySessionId, userGuid);

        return Ok(response);
    }

}