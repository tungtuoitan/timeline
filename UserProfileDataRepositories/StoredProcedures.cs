namespace UserProfileDataRepositories
{
    public static class StoredProcedures
    {
        public static string spSelectUserProfileByEmail => "[dbo].[usp_s_UserProfileByEmail]";
        public static string spInsertUpdateUserProfile=> "[dbo].[usp_iu_UserProfile]";
        public static string spInsertUpdateUser=> "[dbo].[usp_iu_User]";
        public static string spSelectUsers=> "[dbo].[usp_s_Users]";
    }
}
