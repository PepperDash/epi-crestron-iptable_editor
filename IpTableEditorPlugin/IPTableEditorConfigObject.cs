using System.Collections.Generic;
using Newtonsoft.Json;

namespace IPTableEditorPlugin 
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	public class IpTableEditorConfigObject
	{
		/// <summary>
		/// List of IP changes 
		/// </summary>
		[JsonProperty("ipTableChanges")]
		public List<IpTableChangesConfigObject> IpTableChanges { get; set; }
		
		/// <summary>
		/// Run IP Table editor at startup
		/// </summary>
		[JsonProperty("runAtStartup")]		
		public bool RunAtStartup { get; set; } 
	}
	
	/// <summary>
	/// Plugin deviice list object
	/// </summary>
	public class IpTableChangesConfigObject
	{
		/// <summary>
		/// String value representing the object name
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// String value representing the object IP-ID
		/// </summary>
		[JsonProperty("ipId")]
		public string IpId { get; set; }

		/// <summary>
		/// String value representing the object Device ID
		/// </summary>
		[JsonProperty("devId")]
		public string DevId { get; set; }

		/// <summary>
		/// Integer value representing the object IP Port
		/// </summary>
		[JsonProperty("ipPort")]
		public int IpPort { get; set; }

		/// <summary>
		/// String value representing the object IP Address
		/// </summary>
		[JsonProperty("ipAddress")]
		public string IpAddress { get; set; }

        /// <summary>
        /// Integer value representing Program number
        /// </summary>
		[JsonProperty("programNumber")]
		public int ProgramNumber { get; set; }
	}
}