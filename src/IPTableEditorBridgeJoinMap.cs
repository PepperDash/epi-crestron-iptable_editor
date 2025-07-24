using PepperDash.Essentials.Core;

namespace IPTableEditorPlugin 
{
    public class IpTableEditorBridgeJoinMap : JoinMapBaseAdvanced
    {
        /// <summary>
        /// Analog join to report LED product monitor temperature feedback
        /// </summary>
        [JoinName("CheckTable")]
        public JoinDataComplete CheckTable = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 1, 
                    JoinSpan = 10
                },
                new JoinMetadata
                {
                    Description = "Feedback showing if the table needs to be checked, and input to check the table.",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Digital
                });

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="joinStart"></param>
        public IpTableEditorBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(IpTableEditorBridgeJoinMap))
        {
        }
    }

    public class IpTableSelectorBridgeJoinMap : JoinMapBaseAdvanced
    {
        /// <summary>
        /// Analog join to report LED product monitor temperature feedback
        /// </summary>
        [JoinName("SelectItemBool")]
        public JoinDataComplete SelectItemBool = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 1,
                    JoinSpan = 10
                },
                new JoinMetadata
                {
                    Description = "Select and Feedback for selecting each mutable IPTable Entry",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Digital
                });


        /// <summary>
        /// Select and Feedback for selecting each mutable IPTable Entry (Analog join)
        /// </summary>
        [JoinName("SelectItemAnalog")]
        public JoinDataComplete SelectItemAnalog = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 1,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Select and Feedback for selecting each mutable IPTable Entry",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Analog
                });

        /// <inheritdoc/>
        /// <param name="joinStart">The starting join number for the join map.</param>
        /// <param name="ipIdCount">The number of IP table entries to map.</param>
        public IpTableSelectorBridgeJoinMap(uint joinStart, int ipIdCount)
            : base(joinStart, typeof(IpTableSelectorBridgeJoinMap))
        {
            SelectItemBool = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 1,
                    JoinSpan = (uint)ipIdCount
                },
                new JoinMetadata
                {
                    Description = "Select and Feedback for selecting each mutable IPTable Entry",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Digital
                });
        }
    }

}