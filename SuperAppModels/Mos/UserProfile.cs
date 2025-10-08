using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperAppModels.Mos
{
    public class UserProfile
    {
        public string Parents { get; set; }
        public string Priorities { get; set; }
        public string Statuses { get; set; }
        public string Types { get; set; }
        public string RepeatTypes { get; set; }
        public string IsUpdatedTodays { get; set; }

        public UserProfile (){
            Parents = String.Empty;
            Priorities = String.Empty;
            Statuses = String.Empty;
            Statuses = String.Empty;
            Types = String.Empty;
            RepeatTypes = String.Empty;
            IsUpdatedTodays = String.Empty;
        }
        

    }
}
