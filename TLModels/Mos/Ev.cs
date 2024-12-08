using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TLMos.Mos
{ 
    public class Ev
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public string? Type { get; set; }
        public string Level { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public int Status { get; set; }

        public Ev()
        {
            Name = string.Empty;
            Type = string.Empty;
            Level = string.Empty;
        }
    }
}
