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
    public interface IUserProfileRe
    {
        Task<UserProfile> GetUserProfileJson(string email, string appC);
        Task<ResultOptions> IuUserProfile(string email, string appC, string userProfile);
    }
}
