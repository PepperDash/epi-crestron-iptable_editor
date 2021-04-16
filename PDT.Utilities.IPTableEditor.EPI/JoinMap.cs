using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace IPTableEditorEPI
{
    public class IpTableEditorJoinMap : DisplayControllerJoinMap
    {
        /// <summary>
        /// Analog join to report LED product monitor temperature feedback
        /// </summary>
        [JoinName("CheckTable")]
        public JoinDataComplete CheckTable =
            new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
                new JoinMetadata
                {
                    Description = "Show if table needs checked, and check table",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Digital
                });

        public IpTableEditorJoinMap(uint joinStart)
            : base(joinStart, typeof(IpTableEditorJoinMap))
		{
        }


    }

}