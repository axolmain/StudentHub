using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Models;
using StudentHub.Server.Services.DataService;

namespace StudentHub.Server.Controllers;

[ApiController]
[Route("octolearnapi/[controller]")]
public class StudySessionController : Controller
{
    private readonly IDataService _dataService;
    private readonly UserManager<ApplicationUser> _userManager;
    private AuthenticationState authState;

    public StudySessionController(IDataService dataService, UserManager<ApplicationUser> userManager)
    {
        _dataService = dataService;
        _userManager = userManager;
    }

    [HttpPost("makesession")]
    public async Task<IActionResult> MakeSession(string sessionName, string userGuid)
    {
        var file = Request.Form.Files[0];
        string fileName = file.FileName;

        string studySessionId = await _dataService.CreateStudySession(sessionName, userGuid);
        await using var stream = file.OpenReadStream();
        await _dataService.UploadFileAsync(fileName, studySessionId, userGuid, stream);

        return Ok(studySessionId);
    }

    [HttpPost("addfile")]
    public async Task<IActionResult> AddFile([FromForm] IFormFile formFile, [FromForm] string sessionId,
        [FromForm] string userGuid)
    {
        using var stream = formFile.OpenReadStream();
        await _dataService.UploadFileAsync(formFile.FileName, sessionId, userGuid, stream);

        return Ok();
    }

    [HttpPost("getfiles")]
    public async Task<ActionResult<IEnumerable<UserDocument>>> GetFiles(string sessionId, [FromForm] string userGuid)
    {
        var files =
            await _dataService.GetSessionDocuments(userGuid, sessionId);

        return Ok(files);
    }
}