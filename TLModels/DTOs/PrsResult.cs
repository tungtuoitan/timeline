using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.Mos;

namespace TLMos.DTOs
{
    public class PrsResult
    {
        public List<Pr> Prs { get; set; }
        public ResultOptions Options { get; set; }
    }
}
