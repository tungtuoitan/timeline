
using SuperAppModels.DTOs;
using SuperAppModels.Mos;

namespace UserProfileDataServices.Ins
{
    public interface IUserProfileSe
    {
        Task<UserProfile> GetUserProfileJson(string email, string appC);
        Task<ResultOptions> IuUserProfile(string email, string appC, string json);

    }
}