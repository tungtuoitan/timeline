using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperAppModels.Mos
{
    public class FilterModel
    {
        public string Parent {  get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string RepeatType { get; set; }
        public string IsUpdatedToday { get; set; }

        public FilterModel() {
            Parent = string.Empty;
            Priority = string.Empty;
            Status = string.Empty;
            Type = string.Empty;
            RepeatType = string.Empty;
            IsUpdatedToday = string.Empty;
        }

    }
}
