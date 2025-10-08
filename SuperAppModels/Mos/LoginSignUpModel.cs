using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperAppModels.Mos
{
    public class LoginSignUpModel
    {
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Token { get; set; }
        public DateTime? Expire { get; set; }
        public string? Type { get; set; } // "loginDefault" | "signupDefault" | "loginByGoogle

    }
}
