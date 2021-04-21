using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace IPTableEditorPlugin 
{
	/// <summary>
	/// Plugin factory for devices that don't require communications using IBasicCommunications or custom communication methods ** logic only plugin **
	/// </summary>
    public class IpTableEditorFactory : EssentialsPluginDeviceFactory<IpTableEditor>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>>
        public IpTableEditorFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.6.7";
            TypeNames = new List<string> { "IPTableEditor" };
        }

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginTemplateLogicDevice
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
			Debug.Console(1, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

			// get the plugin device properties configuration object & check for null 
			var propertiesConfig = dc.Properties.ToObject<IpTableEditorConfigObject>();
			if (propertiesConfig != null)
			{
				return new IpTableEditor(dc.Key, dc.Name, dc);				
			}

			Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
			return null;            
        }
    }
}