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
}