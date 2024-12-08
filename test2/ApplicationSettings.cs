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
        public static string ERPConnectionString => "Server=157.66.218.17;Database=Timeline;User ID=sa;Password=Noitoibatdau2024#;";
        //public static string ERPConnectionString => "Server=TUNGHOMEPC\\SQLEXPRESS;Database=Timeline;Trusted_Connection=True;";
//#else
//        public static string ERPConnectionString => Environment.GetEnvironmentVariable("ENVIRONMENT") == "Production"
//            ? ConfigurationManager.ConnectionStrings["ERP"].ConnectionString
//                : Environment.GetEnvironmentVariable("ENVIRONMENT") == "UAT"
//                ? ConfigurationManager.ConnectionStrings["ERP-UAT"].ConnectionString
//            : ConfigurationManager.ConnectionStrings["ERP-Dev"].ConnectionString;
//#endif
    }
}
