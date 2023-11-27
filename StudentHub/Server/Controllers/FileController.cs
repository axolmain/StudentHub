using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Services.DataService;

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
    public async Task<IActionResult> Upload(IFormFile file, string sessionName, string userId)
    {
        using var stream = file.OpenReadStream();

        await _dataService.UploadFileAsync(file.FileName, sessionName, userId, stream);

        return Ok();
    }
}