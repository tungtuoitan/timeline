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
    public interface IUserProfileRepositoy
    {
        Task<UserProfile> GetUserProfileJson(string email, string appC);
        Task<ResultOptions> IuUserProfile(string email, string appC, string userProfile);
    }
}
