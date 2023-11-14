using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Server.Models;

namespace StudentHub.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("GetUserId")]
    public Task<ActionResult<string>> GetUserId(string userName)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.UserName == userName);
        return Task.FromResult<ActionResult<string>>(user?.Id);
    }
}