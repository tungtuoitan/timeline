using PLMModels.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace UserProfileDataRes.Ins
{
    public interface IAuthRe
    {
        Task<ResultOptions2<UserModel>> IuUser(UserModel User);
        Task<ResultOptions2<List<UserModel>>> GetUsers();

    }
}
