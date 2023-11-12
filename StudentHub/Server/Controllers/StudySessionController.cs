using StudentHub.Server.Services.DataService;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Models;

namespace StudentHub.Server.Controllers;

[ApiController]
[Route("[controller]")]
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
    public async Task<IActionResult> CreateSession([FromForm] List<IFormFile> files, [FromForm] string sessionName, [FromForm] string userGuid)
    {
        string studySessionId = await _dataService.CreateStudySession(sessionName, userGuid);
        using Stream stream = files[0].OpenReadStream();       
        await _dataService.UploadFileAsync(files[0].FileName, studySessionId, userGuid, stream);

        return Ok(studySessionId);
    }

    [HttpPost("addfile")]
    public async Task<IActionResult> AddFile([FromForm] IFormFile formFile, [FromForm] string sessionId, [FromForm] string userGuid)
    {
        using Stream stream = formFile.OpenReadStream();
        await _dataService.UploadFileAsync(formFile.FileName, sessionId, userGuid, stream);

        return Ok();
    }

    [HttpPost("getfiles")]
    public async Task<ActionResult<IEnumerable<UserDocument>>> GetFiles(string sessionId, [FromForm] string userGuid)
    {
        IEnumerable<UserDocument> files =
            await _dataService.GetSessionDocuments(userGuid, sessionId);

        return Ok(files);
    }
}