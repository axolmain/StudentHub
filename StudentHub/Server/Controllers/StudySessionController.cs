using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Models;
using StudentHub.Server.Services.AiServices;
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
    private readonly ChatAiService chatAiService;


    public StudySessionController(IDataService dataService, UserManager<ApplicationUser> userManager, ChatAiService chatAiService)
    {
        _dataService = dataService;
        _userManager = userManager;        
        this.chatAiService = chatAiService;

    }

    [HttpPost("makesession")]
    [Authorize]
    public async Task<IActionResult> CreateSession([FromForm] List<IFormFile> files, [FromForm] string sessionName, [FromForm] string userId)
    {
        string studySessionId = await _dataService.CreateStudySession(sessionName, userId, files[0].FileName);

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
        
        return Ok(studySession);
    }
    
    [HttpGet("getfilestream")]
    public async Task<Stream> GetFileStrean([FromQuery] string userId, [FromQuery] string sessionId)
    {
        IEnumerable<UserDocument> studySessionDocs = await _dataService.GetSessionDocuments(userId, sessionId);
        if (!studySessionDocs.Any())
            return Stream.Null;

        (Stream File, string FileType) studySessionFile = await _dataService.GetFile(userId, sessionId, studySessionDocs.First().id);
    
        if (studySessionFile.File == null)
        {
            return Stream.Null;
        }


        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = studySessionDocs.First().FileName, // Optional
            Inline = true, // Set to inline
        };
        Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

        return studySessionFile.File;
    }

    
    [HttpGet("getallsessions")]
    public async Task<ActionResult<IEnumerable<StudySession>>> GetAllSessions([FromQuery] string userGuid)
    {
        return Ok(await _dataService.GetStudySessions(userGuid));
    }
    
    [HttpDelete("deletesession/{studySessionId}/{userId}")]
    [Authorize]
    public async Task<IActionResult> DeleteSession(string studySessionId, string userId)
    {
        bool result = await _dataService.DeleteStudySession(studySessionId, userId);
    
        if (result)
        {
            return Ok();
        }
        else
        {
            return StatusCode(500, "Unable to delete the study session.");
        }
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