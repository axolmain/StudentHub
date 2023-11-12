namespace StudentHub.Server.Services;

public class DevUserAuthService : IUserAuthService
{
    public string? GetUserUuid()
    {
        return "_dev3";
    }
}