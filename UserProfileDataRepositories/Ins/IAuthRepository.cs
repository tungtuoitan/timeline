using PLMModels.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperAppModels.DTOs;
using SuperAppModels.Mos;

namespace UserProfileDataRepositories.Ins
{
    public interface IAuthRepository
    {
        Task<ResultOptions2<UserModel>> IuUser(UserModel User);
        Task<ResultOptions2<List<UserModel>>> GetUsers();

    }
}
