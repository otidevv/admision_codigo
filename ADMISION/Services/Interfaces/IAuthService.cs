using ADMISION.Models.ViewModels;

namespace ADMISION.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        Task LogoutAsync();
    }
}
