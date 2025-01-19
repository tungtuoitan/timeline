
using TLMos.DTOs;
using TLMos.Mos;

namespace UserProfileDataSes.Ins
{
    public interface IUserProfileSe
    {
        Task<UserProfile> GetUserProfileJson(string email, string appC);
        Task<ResultOptions> IuUserProfile(string email, string appC, string json);

    }
}