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
//#if DEBUG
        public static string ERPConnectionString => "Server=localhost;Database=Timeline;Trusted_Connection=True;";
//#else
//        public static string ERPConnectionString => Environment.GetEnvironmentVariable("ENVIRONMENT") == "Production"
//            ? ConfigurationManager.ConnectionStrings["ERP"].ConnectionString
//                : Environment.GetEnvironmentVariable("ENVIRONMENT") == "UAT"
//                ? ConfigurationManager.ConnectionStrings["ERP-UAT"].ConnectionString
//            : ConfigurationManager.ConnectionStrings["ERP-Dev"].ConnectionString;
//#endif
    }
}
