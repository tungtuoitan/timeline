

namespace TLMos.Mos
{
    public class Fo
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string IconId { get; set; } = String.Empty;
        public int ParentId { get; set; }

        public string ActiveC { get; set; } = String.Empty;
        public string PrioriC { get; set; } = String.Empty;

        public string? Desc { get; set; }
        public string? Fink { get; set; } = String.Empty;
        public int? PinIndex { get; set; } 
    }
}
