using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins.Crestron.IpTable_Editor 
{
	/// <summary>
	/// Plugin factory for devices that don't require communications using IBasicCommunications or custom communication methods ** logic only plugin **
	/// </summary>
	/// <summary>
	/// Factory class for creating <see cref="IpTableEditor"/> plugin devices.
	/// </summary>
	public class IpTableEditorFactory : EssentialsPluginDeviceFactory<IpTableEditor>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>>
		/// <inheritdoc/>
		public IpTableEditorFactory()
		{
			MinimumEssentialsFrameworkVersion = "2.12.1";
			TypeNames = new List<string> { "IPTableEditor" };
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginTemplateLogicDevice
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <inheritdoc/>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			Debug.LogDebug("[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

            // get the plugin device properties configuration object & check for null 
            var propertiesConfig = dc.Properties.ToObject<IpTableEditorConfigObject>();
			if (propertiesConfig == null)
			{
				Debug.LogInformation("[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
				return null;
			}

			IBasicCommunication comm = null;
			
			// Only attempt to create communication if control properties exist
			if (propertiesConfig.Control != null)
			{
				try
				{
					comm = CommFactory.CreateCommForDevice(dc);
					Debug.LogDebug( "[{0}] Factory: Communication object created successfully", dc.Key);
				}
				catch (System.Exception ex)
				{
					Debug.LogInformation("[{0}] Factory: Failed to create communication object. Error: {1}", dc.Key, ex.Message);
				}
			}
			else
			{
				Debug.LogDebug( "[{0}] Factory: No control properties found - using logic-only mode", dc.Key);
			}

			return comm == null ? new IpTableEditor(dc.Key, dc.Name, dc) : new IpTableEditor(dc.Key, dc.Name, dc, comm);			
		}
	}
}