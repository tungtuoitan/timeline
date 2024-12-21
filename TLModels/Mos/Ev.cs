

namespace TLMos.Mos
{ 
    public class Ev
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Type { get; set; }
        public string LevelC { get; set; }

        public DateTime TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public int? ParentId { get; set; }

        public string ActiveC { get; set; }
        public string StatusC { get; set; }
        public string PrioriC { get; set; }

        public Ev()
        {
            Name = string.Empty;
            LevelC = string.Empty;
        }
    }
}
