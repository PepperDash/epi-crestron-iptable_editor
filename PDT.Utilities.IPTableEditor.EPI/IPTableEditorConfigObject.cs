using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace IPTableEditorEPI
{
	public class IPTableEditorConfigObject
	{
		public List<IPTableObject> IPTableChanges { get; set; } 
	}
	
	public class IPTableObject
	{
		public string IpId { get; set; }
		public string DevID { get; set; }
		public int IpPort { get; set; }
		public string IpAddress { get; set; }
		public int ProgramNumber { get; set; }
	}
}