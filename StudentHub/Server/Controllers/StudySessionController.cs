using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Models;
using StudentHub.Server.Services.DataService;
using StudentHub.Shared;

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
    [Authorize]
    public async Task<IActionResult> CreateSession([FromForm] List<IFormFile> files, [FromForm] string sessionName, [FromForm] string userId)
    {
        string studySessionId = await _dataService.CreateStudySession(sessionName, userId);

        foreach (IFormFile file in files)
        {
            using Stream stream = file.OpenReadStream();

            await _dataService.UploadFileAsync(file.FileName, studySessionId, userId, stream);
        }

        return Ok(studySessionId);
    }
    
    
    [HttpGet("getsession")]
    public async Task<IActionResult> GetSession([FromQuery] string userId, [FromQuery] string sessionId)
    {
        var studySession = await _dataService.GetStudySession(userId, sessionId);
        string studySessionId = studySession.id;

        return Ok(studySession);
    }


    
    [HttpGet("getallsessions")]
    public async Task<ActionResult<IEnumerable<StudySession>>> GetAllSessions([FromQuery] string userGuid)
    {
        return Ok(await _dataService.GetStudySessions(userGuid));
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