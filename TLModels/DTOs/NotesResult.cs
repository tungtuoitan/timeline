using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.Mos;

namespace TLMos.DTOs
{
    public class NotesResult
    {
        public List<Note> Notes { get; set; }
        public ResultOptions Options { get; set; }
    }
}