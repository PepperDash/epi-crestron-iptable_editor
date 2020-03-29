using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;

namespace IPTableEditorEPI
{
	public static class IPTableEditorApiExtensions
	{

		public static void LinkToApiExt(this IPTableEditor IptDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			IPTableEditorTemplateBridgeJoinMap joinMap = new IPTableEditorTemplateBridgeJoinMap();

			var JoinMapSerialized = JoinMapHelper.GetJoinMapForDevice(joinMapKey);
			
			if (!string.IsNullOrEmpty(JoinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<IPTableEditorTemplateBridgeJoinMap>(JoinMapSerialized);

			joinMap.OffsetJoinNumbers(joinStart);
			Debug.Console(2, IptDevice, "Bridge | JoinStart: {0}", joinStart);
			Debug.Console(1, IptDevice, "Bridge | Linking To Trilist: {0}", trilist.ID.ToString("X"));
			for (int i = 0; i < 10; i++)
			{
				trilist.SetSigTrueAction( (ushort)(joinMap.CheckTable + i), () => {IptDevice.CheckTables(i + 1);} );
				IptDevice.HasModsFeedback[i + 1].LinkInputSig(trilist.BooleanInput[ (ushort)(joinMap.CheckTable + 1)] );
			}
		}
	}
	public class IPTableEditorTemplateBridgeJoinMap : JoinMapBase
	{
		public uint CheckTable { get; set; }
		public IPTableEditorTemplateBridgeJoinMap() 
		{
			CheckTable = 1;
		}

		public override void OffsetJoinNumbers(uint joinStart)
		{
			var Offset = joinStart - 1;
			CheckTable += Offset;
		}

	}
}