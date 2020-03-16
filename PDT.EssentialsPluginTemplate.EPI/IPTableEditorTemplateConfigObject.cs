using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace IPTableEditorTemplateEPI
{
	public class IPTableEditorTemplateConfigObject
	{
		public List<IPTableObject> IPTableChanges { get; set; } 
	}
	
	public class IPTableObject
	{
		public string IpId { get; set; }
		public string IpAddress { get; set; }
		public int ProgramNumber { get; set; }
	}
}