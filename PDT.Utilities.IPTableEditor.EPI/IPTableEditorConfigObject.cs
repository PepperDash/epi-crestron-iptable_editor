using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace IPTableEditorEPI
{
	public class IPTableEditorConfigObject
	{
		public List<IPTableObject> IPTableChanges { get; set; }
		[JsonProperty("runAtStartup")]
		public bool RunAtStartup { get; set; } 
	}
	
	public class IPTableObject
	{
		[JsonProperty("ipId")]
		public string IpId { get; set; }

		[JsonProperty("devId")]
		public string DevID { get; set; }

		[JsonProperty("ipPort")]
		public int IpPort { get; set; }

		[JsonProperty("ipAddress")]
		public string IpAddress { get; set; }

		[JsonProperty("programNumber")]
		public int ProgramNumber { get; set; }


	}
}