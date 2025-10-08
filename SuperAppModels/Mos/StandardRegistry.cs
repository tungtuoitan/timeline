namespace SuperAppModels.Mos
{ 
    public class StandardRegistry
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Desc { get; set; }
        public string Type { get; set; }
        public int Active { get; set; }

        public StandardRegistry()
        {
            Code = string.Empty;
            Desc = string.Empty;
            Type = string.Empty;
        }
    }
}
