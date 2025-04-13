using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLMos.Mos
{
    public class UserModel
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Password { get; set; }
        public string? Token { get; set; }
        public DateOnly? Expire { get; set; }
        public string? Type { get; set; } // "loginDefault" | "signupDefault" | "loginByGoogle
    };

    }
