using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;


namespace PepperDash.Essentials.Plugins.Crestron.IpTable_Editor 
{
    /// <summary>
    /// Plugin device configuration object
    /// </summary>
    /// <summary>
    /// Configuration object for the IP Table Editor plugin.
    /// </summary>
    public class IpTableEditorConfigObject
    {
        /// <summary>
        /// List of IP changes 
        /// </summary>
        [JsonProperty("ipTableChanges")]
        /// <inheritdoc/>
        public List<IpTableChangesConfigObject> IpTableChanges { get; set; }
        
        /// <summary>
        /// Run IP Table editor at startup
        /// </summary>
        [JsonProperty("runAtStartup")]		
        /// <inheritdoc/>
        public bool RunAtStartup { get; set; }

        [JsonProperty("control")]
        /// <inheritdoc/>
        public EssentialsControlPropertiesConfig Control { get; set; }

        [JsonProperty("persistentEntry")]
        /// <inheritdoc/>
        public IpTableObjectBase PersistentEntry { get; set; }

        [JsonProperty("selectableEntries")]
        /// <inheritdoc/>
        public Dictionary<int, IpTableObjectBase> SelectableEntries { get; set; }

        /// <inheritdoc/>
        public IpTableEditorConfigObject()
        {
            IpTableChanges = new List<IpTableChangesConfigObject>();
            SelectableEntries = new Dictionary<int, IpTableObjectBase>();
        }

    }

    /// <summary>
    /// Base class for IP Table objects, representing a device entry in the IP table.
    /// </summary>
    public class IpTableObjectBase
    {

        /// <summary>
        /// String value representing the object IP-ID
        /// </summary>
        [JsonProperty("ipId")]
        /// <summary>
        /// Gets or sets the IP-ID for the device.
        /// </summary>
        public string IpId { get; set; }

        /// <summary>
        /// String value representing the object IP Address
        /// </summary>
        [JsonProperty("ipAddress")]
        /// <summary>
        /// Gets or sets the IP address for the device.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// String Value representing optional room ID
        /// </summary>
        [JsonProperty("roomId", NullValueHandling = NullValueHandling.Ignore)]
        /// <summary>
        /// Gets or sets the optional room ID for the device.
        /// </summary>
        public string RoomId { get; set; }

    }

    
    /// <summary>
    /// Plugin deviice list object
    /// </summary>
    /// <summary>
    /// Represents a change entry for the IP Table Editor, including device and program info.
    /// </summary>
    public class IpTableChangesConfigObject : IpTableObjectBase
    {
        /// <summary>
        /// String value representing the object name
        /// </summary>
        [JsonProperty("name")]
        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// String value representing the object Device ID
        /// </summary>
        [JsonProperty("devId")]
        /// <summary>
        /// Gets or sets the device ID for the device.
        /// </summary>
        public string DevId { get; set; }

        /// <summary>
        /// Integer value representing the object IP Port
        /// </summary>
        [JsonProperty("ipPort")]
        /// <summary>
        /// Gets or sets the IP port for the device.
        /// </summary>
        public int IpPort { get; set; }


        /// <summary>
        /// Integer value representing Program number
        /// </summary>
        [JsonProperty("programNumber")]
        /// <summary>
        /// Gets or sets the program number for the device.
        /// </summary>
        public int ProgramNumber { get; set; }
    }


}