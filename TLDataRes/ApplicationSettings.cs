using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLDataRes
{
    public static class ApplicationSettings
    {
#if DEBUG
        public static string ERPConnectionString => "Server=157.66.218.17;Database=Timeline-dev;User ID=sa;Password=Noitoibatdau2024#;";
#else
        public static string ERPConnectionString => Environment.GetEnvironmentVariable("ENVIRONMENT") == "pro"
            ?  "Server=157.66.218.17;Database=Timeline-pro;User ID=sa;Password=Noitoibatdau2024#;";
            :  "Server=157.66.218.17;Database=Timeline-dev;User ID=sa;Password=Noitoibatdau2024#;";
#endif
    }
}
