using Backend.Services;
using Backend.Services.DataService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class StudySessionController : Controller
{
    private readonly IDataService _dataService;
    private readonly IUserAuthService _userAuthService;

    public StudySessionController(
        IDataService dataService,
        IUserAuthService userAuthService)
    {
        _dataService = dataService;
        _userAuthService = userAuthService;
    }

    [HttpPost("makesession")]
    [Authorize]
    public async Task<IActionResult> CreateSession([FromForm] List<IFormFile> files, [FromForm] string sessionName)
    {
        string studySessionId = await _dataService.CreateStudySession(sessionName, _userAuthService.GetUserUuid());

        foreach (IFormFile file in files)
        {
            using Stream stream = file.OpenReadStream();

            await _dataService.UploadFileAsync(file.FileName, studySessionId, _userAuthService.GetUserUuid(), stream);
        }

        return Ok(studySessionId);
    }

    [HttpPost("getsessions")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<StudySession>>> GetSessions()
    {
        return Ok(await _dataService.GetStudySessions(_userAuthService.GetUserUuid()));
    }

    [HttpPost("addfile")]
    [Authorize]
    public async Task<IActionResult> AddFile([FromForm] IFormFile formFile, string sessionId)
    {
        using Stream stream = formFile.OpenReadStream();

        await _dataService.UploadFileAsync(formFile.FileName, sessionId, _userAuthService.GetUserUuid(), stream);

        return Ok();
    }

    [HttpPost("getfiles")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserDocument>>> GetFiles(string sessionId)
    {
        IEnumerable<UserDocument> files =
            await _dataService.GetSessionDocuments(_userAuthService.GetUserUuid(), sessionId);

        return Ok(files);
    }
}