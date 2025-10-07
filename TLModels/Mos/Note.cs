namespace TLMos.Mos
{
    public class Note
    {
        public int NoteId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public string? Type { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; }

        public Note()
        {
            Name = string.Empty;
            CreatedAt = DateTime.Now;
            IsArchived = false;
        }
    }
}