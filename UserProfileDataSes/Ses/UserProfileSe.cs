
using TLMos.Mos;
using TLMos.DTOs;
using Microsoft.AspNetCore.Http;
using PRDataRes.Ins;
using UserProfileDataSes.Ins;
using UserProfileDataRes.Ins;

namespace UserProfileDataSes.Ses
{
    public class UserProfileSe: IUserProfileSe
    {
        private readonly IUserProfileRe _XRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserProfileSe(IUserProfileRe XRepo, IHttpContextAccessor httpContextAccessor)
        {
            _XRepo = XRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<UserProfile> GetUserProfileJson(string email, string appC)
        {
            UserProfile userProfile = await _XRepo.GetUserProfileJson(email, appC);
            return userProfile;
        }
        public async Task<ResultOptions> IuUserProfile(string email, string appC, string userProfileJson)
        {
            return await _XRepo.IuUserProfile(email, appC, userProfileJson);
        }
    }
}
