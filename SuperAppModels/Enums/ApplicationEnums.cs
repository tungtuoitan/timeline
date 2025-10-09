namespace SuperAppModels.Enums
{
    /// <summary>
    /// Authentication types supported by the application
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// Local authentication (email/phone + password)
        /// </summary>
        Local = 0,

        /// <summary>
        /// Google OAuth authentication
        /// </summary>
        Google = 1,

        /// <summary>
        /// Facebook OAuth authentication (future)
        /// </summary>
        Facebook = 2,

        /// <summary>
        /// Microsoft OAuth authentication (future)
        /// </summary>
        Microsoft = 3,

        /// <summary>
        /// Apple OAuth authentication (future)
        /// </summary>
        Apple = 4
    }

    /// <summary>
    /// Note status enumeration
    /// </summary>
    public enum NoteStatus
    {
        /// <summary>
        /// Active note
        /// </summary>
        Active = 0,

        /// <summary>
        /// Archived note
        /// </summary>
        Archived = 1,

        /// <summary>
        /// Deleted note (soft delete)
        /// </summary>
        Deleted = 2
    }

    /// <summary>
    /// User account status
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// Active user account
        /// </summary>
        Active = 0,

        /// <summary>
        /// Inactive user account
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Suspended user account
        /// </summary>
        Suspended = 2,

        /// <summary>
        /// Deleted user account (soft delete)
        /// </summary>
        Deleted = 3
    }

    /// <summary>
    /// Standard registry types
    /// </summary>
    public enum RegistryType
    {
        /// <summary>
        /// System configuration
        /// </summary>
        System = 0,

        /// <summary>
        /// User preferences
        /// </summary>
        UserPreference = 1,

        /// <summary>
        /// Application settings
        /// </summary>
        AppSetting = 2,

        /// <summary>
        /// Feature flags
        /// </summary>
        FeatureFlag = 3,

        /// <summary>
        /// Lookup values
        /// </summary>
        Lookup = 4
    }
}