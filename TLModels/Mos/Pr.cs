

namespace TLMos.Mos
{
    public class Pr
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public string Types { get; set; }
        public string RepeatType { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string ActiveC { get; set; }
        public string StatusC { get; set; }
        public string PrioriC { get; set; }
        public string? Fink { get; set; }
        public string? Desc { get; set; }
        public string Pesults { get; set; }
        public string KnowC { get; set; }
        public string KnowLevelC { get; set; }


        public Pr()
        {
            Name = string.Empty;
        }
    }
}
