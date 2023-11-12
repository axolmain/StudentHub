using Backend.Services.DataService;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

public class FileController : ControllerBase
{
    private readonly IDataService _dataService;

    public FileController(IDataService dataService)
    {
        _dataService = dataService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, string sessionId, string userId)
    {
        using Stream stream = file.OpenReadStream();

        await _dataService.UploadFileAsync(file.FileName, sessionId, userId, stream);

        return Ok();
    }

    [HttpPost("makesession")]
    public async Task<IActionResult> MakeSession([FromForm] string sessionName, [FromForm] string userId)
    {
        await _dataService.CreateStudySession(sessionName, userId);

        return Ok();
    }
}