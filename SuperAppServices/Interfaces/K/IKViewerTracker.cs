namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Tracks which users are currently "viewing" the K repo diff popup on the FE.
    /// While a user is viewing, the BE sync daemon must NOT push DB → remote nor
    /// re-compare remote vs DB — the diff entries the user is inspecting must stay
    /// stable until they click Apply or Cancel.
    ///
    /// <para>Tracking is per-connection (a single user may open multiple tabs); a
    /// user is "viewing" while at least one of their connections has called
    /// <see cref="StartViewing"/> without a matching <see cref="StopViewing"/>.</para>
    /// </summary>
    public interface IKViewerTracker
    {
        void StartViewing(int userId, string connectionId);
        void StopViewing(int userId, string connectionId);
        /// <summary>Best-effort cleanup when a SignalR connection drops without StopViewing.</summary>
        void OnDisconnect(string connectionId);
        bool IsViewing(int userId);
    }
}
