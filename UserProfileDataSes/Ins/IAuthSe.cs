
using PLMModels.DTOs;
using TLMos.DTOs;
using TLMos.Mos;

namespace UserProfileDataSes.Ins
{
    public interface IAuthSe
    {
        Task<ResultOptions2<UserModel>> IuUser(UserModel model);
        Task<ResultOptions2<List<UserModel>>> GetUsers();

    }
}