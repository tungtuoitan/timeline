namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Batch update test nodes:
    ///   - AddNodeIds: k.node.id list to add as new questions
    ///   - ToggleTestNodeIds: k.test_node.id list to flip isActive
    ///   - DeleteTestNodeIds: k.test_node.id list to permanently remove
    /// </summary>
    public class KUpdateTestNodesRequest
    {
        /// <summary>k.node IDs to add as new active questions</summary>
        public List<int> AddNodeIds { get; set; } = [];

        /// <summary>k.test_node IDs whose IsActive to toggle (true→false or false→true)</summary>
        public List<int> ToggleTestNodeIds { get; set; } = [];

        /// <summary>k.test_node IDs to permanently delete from this test</summary>
        public List<int> DeleteTestNodeIds { get; set; } = [];
    }
}
