
using TLMos.DTOs;
using TLMos.Mos;

namespace UserProfileDataSes.Ins
{
    public interface IAuthSe
    {
        Task<UserModel> GenerateJwtToken(UserModel model);

    }
}