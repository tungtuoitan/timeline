using SuperAppModels.Models;
using SuperAppModels.DTOs;
using Microsoft.AspNetCore.Http;
using UserProfileDataServices.Ins;
using UserProfileDataRepositories.Ins;

namespace UserProfileDataServices.Services
{
    public class UserProfileSe: IUserProfileSe
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public UserProfileSe(IUserProfileRepository userProfileRepository, IHttpContextAccessor httpContextAccessor)
        {
            _userProfileRepository = userProfileRepository;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public async Task<UserProfile?> GetUserProfileByEmailAsync(string email)
        {
            return await _userProfileRepository.GetUserProfileByEmailAsync(email);
        }
        
        public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile)
        {
            return await _userProfileRepository.CreateUserProfileAsync(userProfile);
        }
        
        public async Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            return await _userProfileRepository.UpdateUserProfileAsync(userProfile);
        }
        
        public async Task<bool> DeleteUserProfileAsync(int id)
        {
            return await _userProfileRepository.DeleteUserProfileAsync(id);
        }
    }
}
