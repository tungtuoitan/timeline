
using SuperAppModels.Mos;
using SuperAppModels.DTOs;
using Microsoft.AspNetCore.Http;
using UserProfileDataServices.Ins;
using UserProfileDataRepositories.Ins;

namespace UserProfileDataServices.Services
{
    public class UserProfileSe: IUserProfileSe
    {
        private readonly IUserProfileRepositoy _XRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserProfileSe(IUserProfileRepositoy XRepository, IHttpContextAccessor httpContextAccessor)
        {
            _XRepository = XRepository;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<UserProfile> GetUserProfileJson(string email, string appC)
        {
            UserProfile userProfile = await _XRepository.GetUserProfileJson(email, appC);
            return userProfile;
        }
        public async Task<ResultOptions> IuUserProfile(string email, string appC, string userProfileJson)
        {
            return await _XRepository.IuUserProfile(email, appC, userProfileJson);
        }
    }
}
