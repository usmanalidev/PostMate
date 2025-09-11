using PostmateAPI.DTOs;

namespace PostmateAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(string username);
        bool ValidateCredentials(string username, string password);
    }
}
