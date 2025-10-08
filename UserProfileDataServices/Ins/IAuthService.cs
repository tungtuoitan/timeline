
using PLMModels.DTOs;
using SuperAppModels.DTOs;
using SuperAppModels.Mos;

namespace UserProfileDataServices.Ins
{
    public interface IAuthService
    {
        Task<ResultOptions2<UserModel>> IuUser(UserModel model);
        Task<ResultOptions2<List<UserModel>>> GetUsers();

    }
}