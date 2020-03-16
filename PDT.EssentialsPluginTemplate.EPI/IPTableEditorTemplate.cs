using System;
using System.Text.RegularExpressions;
using System.Linq;

using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;

namespace IPTableEditorTemplateEPI 
{
	public class IPTableEditorTemplate : Device
	{

		public static void LoadPlugin()
		{
			PepperDash.Essentials.Core.DeviceFactory.AddFactoryForType("IPTableEditorTemplate", IPTableEditorTemplate.BuildDevice);	
		}

		public static IPTableEditorTemplate BuildDevice(DeviceConfig dc)
		{
			var config = JsonConvert.DeserializeObject<IPTableEditorTemplateConfigObject>(dc.Properties.ToString());
			var newMe = new IPTableEditorTemplate(dc.Key, dc.Name, config);
			return newMe;
		}

		string myResponse; 
		IPTableEditorTemplateConfigObject Config;
		Dictionary<int, bool> RebootSlotList;

		public IPTableEditorTemplate(string key, string name, IPTableEditorTemplateConfigObject config)
			: base(key, name)
		{
			Config = config;
			RebootSlotList = new Dictionary<int, bool>();
		}

		public void CheckTables()
		{
			foreach(var ipChange in Config.IPTableChanges)
			{
				var consoleCommand = String.Format("IPT -p:{0} -I: {1} -T", ipChange.ProgramNumber, ipChange.IpId);
				var consoleResponse = CrestronConsole.SendControlSystemCommand(consoleCommand, ref myResponse) ? myResponse : null;
				Debug.Console(2, "CheckTables Response:{0}\n", myResponse);
				var myResponseSplit = myResponse.Split('|');
				var currentIP = myResponseSplit[5];
				var changeIP = NormalizeIpAddress(ipChange.IpAddress);
				Debug.Console(2, "CheckTables Current:{0} Change:{1}\n", currentIP, changeIP);
				if (currentIP == changeIP)
				{
					Debug.Console(2, "CheckTables No Change Nessecery {0}", ipChange);
				}
				else
				{
					SendIptCommand(ipChange);
				}
			}
		}


		private string NormalizeIpAddress(string data)
		{
			//Normalizes all ip addresses to utilize three digits in each octet
			//Passes hostnames directly out without manipulation
			Debug.Console(2, this, "Normalize Address Input = {0}", data);
			//remove "(not resolved)" from unresolved hostnames in the IPTable entry list.
			data = Regex.Replace(data, @"\(([^)]*)\)$", "");

			string[] myOctetsArray = data.Split('.');
			List<string> myOctets = myOctetsArray.OfType<string>().ToList();
			if (myOctets.Count() == 4)
			{
				for (int i = 0; i < myOctets.Count(); i++)
				{
					char[] charArray = myOctets[i].ToCharArray();
					if (!charArray.All(char.IsDigit))
					{
						//If an index contains a non-numeric character, it's a hostname
						Debug.Console(2, this, "Normalize Address Return = {0}", data);
						return data;
					}
					if (myOctets[i].Length > 3)
					{
						//If an index has more than three digits, it's a hostname
						Debug.Console(2, this, "Normalize Address Return = {0}", data);
						return data;
					}
					else
						//make sure each index has at least 3 digits - may break some hostnames
						myOctets[i] = myOctets[i].PadLeft(3, '0');
				}
				var myReturn = string.Join(".", myOctets.ToArray());
				Debug.Console(2, this, "Normalize Address Return = {0}", myReturn);
				return myReturn;
			}
			else
				Debug.Console(2, this, "Normalize Address Return = {0}", data);
			return data;
		}

		private void SendIptCommand(IPTableObject data)
		{
			if (data != null)
			{
				string ConsoleResponse = "";

				var deviceDeclaration = !String.IsNullOrEmpty(data.IpId) ? String.Format("-D:{0}", data.IpAddress) : "";
				var programDeclaration = String.Format("-P:{0}", data.ProgramNumber);

				var iptCommand = String.Format("addp {0} {1} {2} {3}", data.IpId, data.IpAddress, programDeclaration, deviceDeclaration);

				Debug.Console(2, this, "IPID Command Sent : {0}", iptCommand);
				if (CrestronConsole.SendControlSystemCommand(iptCommand, ref ConsoleResponse))
				{
					if (ConsoleResponse.ToLower().Contains("restart program"))
					{
						Debug.Console(2, "Success IPT Entry Changed{0}", data);
						if (!RebootSlotList.ContainsKey(data.ProgramNumber))
						{
							RebootSlotList.Add(data.ProgramNumber, true);
						}
					}
				}
			}
		}
	}
}

