namespace SuperAppDataRepositories
{
    public static class StoredProcedures
    {
        public static string spSelectEvs=> "[dbo].[usp_s_Evs]";
        public static string spInsertUpdateEv=> "[dbo].[usp_iu_Ev]";
        public static string spSelectStandardRegistries=> "[dbo].[usp_s_SRs]";
        public static string spSelectNotes=> "[dbo].[usp_s_Notes]";
        public static string spInsertUpdateNote=> "[dbo].[usp_iu_Note]";
    }
}
