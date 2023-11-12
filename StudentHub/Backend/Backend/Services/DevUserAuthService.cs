namespace Backend.Services;

public class DevUserAuthService : IUserAuthService
{
    public string? GetUserUuid()
    {
        return "_dev3";
    }
}