using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;


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

        [JsonProperty("control")]
        public EssentialsControlPropertiesConfig Control { get; set; }

        [JsonProperty("persistentEntry")]
        public IpTableObjectBase PersistentEntry { get; set; }

        [JsonProperty("selectableEntries")]
        public Dictionary<int, IpTableObjectBase> SelectableEntries { get; set; }

	}

    public class IpTableObjectBase
    {

        /// <summary>
        /// String value representing the object IP-ID
        /// </summary>
        [JsonProperty("ipId")]
        public string IpId { get; set; }

        /// <summary>
        /// String value representing the object IP Address
        /// </summary>
        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        /// <summary>
        /// String Value representing optional room ID
        /// </summary>
        [JsonProperty("roomId", NullValueHandling = NullValueHandling.Ignore)]
        public string RoomId { get; set; }

    }

	
	/// <summary>
	/// Plugin deviice list object
	/// </summary>
    public class IpTableChangesConfigObject : IpTableObjectBase
	{
        /// <summary>
        /// String value representing the object name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }


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
        /// Integer value representing Program number
        /// </summary>
		[JsonProperty("programNumber")]
		public int ProgramNumber { get; set; }
	}


}