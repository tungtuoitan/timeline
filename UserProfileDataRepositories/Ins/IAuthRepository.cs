using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace UserProfileDataRepositories.Ins
{
    public interface IAuthRepository
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> SignupAsync(SignupRequest request);
        Task<AuthResponse> GoogleAuthAsync(GoogleCodeRequest request);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        
        // Legacy methods for backward compatibility
        Task<ResultOptions2<UserModel>> IuUser(UserModel user);
        Task<ResultOptions2<List<UserModel>>> GetUsers();
    }
}
