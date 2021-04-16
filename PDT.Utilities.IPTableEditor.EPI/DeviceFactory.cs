using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;
using System.Collections.Generic;
using PepperDash.Essentials.DM.AirMedia;

namespace IPTableEditorEPI
{
    public class IpTableEditorFactory : EssentialsPluginDeviceFactory<IpTableEditor>
    {
        public IpTableEditorFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.6.7";

            TypeNames = new List<string> { "IPTableEditor" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new IPTable Editor");

            return new IpTableEditor(dc.Key, dc.Name, dc);
        }
    }
}