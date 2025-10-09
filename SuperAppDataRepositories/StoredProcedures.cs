namespace SuperAppDataRepositories
{
    public static class StoredProcedures
    {
        public static string spSelectEvs=> "[dbo].[usp_s_Evs]";
        public static string spInsertUpdateEv=> "[dbo].[usp_iu_Ev]";
        public static string spSelectStandardRegistries=> "[dbo].[usp_s_SRs]";
        public static string spSelectNotes=> "[dbo].[usp_s_Notes]";
        public static string spInsertUpdateNote=> "[dbo].[usp_iu_Note]";
        public static string spDeleteNote=> "[dbo].[usp_d_Note]";
        public static string spSelectNoteById=> "[dbo].[usp_s_NoteById]";
        public static string spSelectStandardRegistryById=> "[dbo].[usp_s_SRById]";
        public static string spSelectStandardRegistryByKey=> "[dbo].[usp_s_SRByKey]";
        public static string spInsertUpdateStandardRegistry=> "[dbo].[usp_iu_SR]";
        public static string spDeleteStandardRegistry=> "[dbo].[usp_d_SR]";
    }
}
