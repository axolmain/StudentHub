using Microsoft.AspNetCore.Authorization;
using StudentHub.Server.Services.DataService;
using Microsoft.AspNetCore.Mvc;

namespace StudentHub.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
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

        await _dataService.UploadFileAsync(file.FileName, "SingleSession", userId, stream);

        return Ok();
    }

    [HttpPost("makesession")]
    public async Task<IActionResult> MakeSession([FromForm] string sessionName, [FromForm] string userId)
    {
        await _dataService.CreateStudySession("SingleSession", userId);

        return Ok();
    }
}