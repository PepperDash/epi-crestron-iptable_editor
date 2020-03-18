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
	public static class IPTableEditorBridge
	{

		public static void LinkToApiExt(this IPTableEditor DspDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			IPTableEditorTemplateBridgeJoinMap joinMap = new IPTableEditorTemplateBridgeJoinMap(joinStart);

			var JoinMapSerialized = JoinMapHelper.GetJoinMapForDevice(joinMapKey);
			
			if (!string.IsNullOrEmpty(JoinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<IPTableEditorTemplateBridgeJoinMap>(JoinMapSerialized);


		}
	}
	public class IPTableEditorTemplateBridgeJoinMap : JoinMapBase
	{
		public IPTableEditorTemplateBridgeJoinMap(uint joinStart) 
		{
			OffsetJoinNumbers(joinStart);
		}

		public override void OffsetJoinNumbers(uint joinStart)
		{
            GetType()
                .GetCType()
                .GetProperties()
                .Where(x => x.PropertyType == typeof(uint))
                .ToList()
                .ForEach(prop => prop.SetValue(this, (uint)prop.GetValue(this, null) + joinStart - 1, null));
		}

	}
}