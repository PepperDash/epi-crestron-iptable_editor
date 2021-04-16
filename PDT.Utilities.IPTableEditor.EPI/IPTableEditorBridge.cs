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

		public static void LinkToApiExt(this IpTableEditor IptDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			
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