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

namespace IPTableEditorEPI 
{
	public class IPTableEditor : Device
	{

		public static void LoadPlugin()
		{
			PepperDash.Essentials.Core.DeviceFactory.AddFactoryForType("IPTableEditor", IPTableEditor.BuildDevice);	
		}

		//public static string MinimumEssentialsFrameworkVersion = "1.4.31";

		public static IPTableEditor BuildDevice(DeviceConfig dc)
		{
			var config = JsonConvert.DeserializeObject<IPTableEditorConfigObject>(dc.Properties.ToString());
			var newMe = new IPTableEditor(dc.Key, dc.Name, config);
			return newMe;
		}

		string myResponse;
		private bool isInitialized;
		private int rebootCount;
		private int rebootCountLimit = 3;
		IPTableEditorConfigObject Config;
		Dictionary<int, bool> RebootSlotList;
		Dictionary<int, List<IPTableObject>> SortedMods = new Dictionary<int, List<IPTableObject>>();

		public IPTableEditor(string key, string name, IPTableEditorConfigObject config)
			: base(key, name)
		{
			Config = config;
			RebootSlotList = new Dictionary<int, bool>();
			//this.CheckTables();
			AddPostActivationAction(this.SortMods);			
		}

		
		// 2020-03-25 ERD If we get a response from a new program being started, if its a program slot that we are currently interested in, then CheckTables()
		//if (Crestron.SimplSharp.eProgramStatusEventType[Config.IPTableChanges] = 2])
		//foreach(var progStart in ConfigPropertiesHelpers.

		//
		
		private void SortMods()
		{
			for (int i = 1; i < 11; i++) 
			{
				var LocalMods = new List<IPTableObject>();
				var selected = Config.IPTableChanges.Where(item => item.ProgramNumber == i);
				if (selected != null)
				{
					Config.IPTableChanges = Config.IPTableChanges.Except(selected).ToList();
					LocalMods.AddRange(selected);
					Debug.Console(2, this, "SortMods | Adding {0} mods to slot {1}", LocalMods.Count, i);
					SortedMods.Add(i, LocalMods);
				}
				else
				{
					Debug.Console(2, this, "SortMods | Slot {0} had no modes}", i);
				}
			}
		}


		public void CheckTables(int slot)
		{
			List<IPTableObject> localIPTableObject = new List<IPTableObject>();
			SortedMods.TryGetValue(slot, out localIPTableObject);
			if (localIPTableObject != null)
			{
				foreach (var ipChange in localIPTableObject)
				{
					var consoleCommand = String.Format("IPT -p:{0} -I: {1} -T", ipChange.ProgramNumber, ipChange.IpId);
					var consoleResponse = CrestronConsole.SendControlSystemCommand(consoleCommand, ref myResponse) ? myResponse : null;
					Debug.Console(2, this, "CheckTables Response:{0}\n", myResponse);
					var myResponseByLine = Regex.Split(myResponse, "\r\n");	// Ignore first line: CIP_ID  |Type    |Status    |DevID   |Port   |IP Address/SiteName       |Model Name          |Description         |RoomId
					//divide the return by line
					List<string> responseList = myResponseByLine.OfType<string>().ToList();
					//convert the array to a list
					responseList.RemoveRange(0, 4);
					//Removes the first two lines - this is junk data
					responseList.RemoveRange(responseList.Count - 2, 2);
					//Removes the final two lines - This is junk data
					if (responseList.Count > 0)
					{
						var myResponseSplit = responseList[0].Split('|');
						var currentIPID = myResponseSplit[0].Trim();
						var currentDeviceID = myResponseSplit[3].Trim();
						var currentPort = myResponseSplit[4].Trim();
						var currentIpAddress = myResponseSplit[5].Trim();

						// TODO 2020-03-18 ERD : Was working on getting the DeviceID and IP Port from config to compare and write if necessary.

						// Normalize entries from Config
						// if (!String.IsNullOrEmpty(ipChange.DevID)) { } //2020-03-25 ERD Attempt to check DeviceID here instead of 'Normalizing' method'. Taken from Technics Core. Would need to set an 'editFlag'
						var changeDeviceID = NormalizeDeviceID(ipChange.DevID);
						//var changeIpPort = FlagIpPort(ipChange.IpPort);
						var changeIpAddress = NormalizeIpAddress(ipChange.IpAddress);

						Debug.Console(2, this, "CheckTables Current:{0} Change:{1}\n", currentIpAddress, changeIpAddress);
						if (currentIpAddress == changeIpAddress)
						{
							Debug.Console(2, this, "CheckTables No Change Necessary {0}", ipChange);
						}
						else
						{
							SendIptCommand(ipChange);
						}
					}
					else
					{
						Debug.Console(2, this, "CheckTables No Current Entry. Send IPT Command", ipChange);
						SendIptCommand(ipChange);
					}
				}
			}

/*
			foreach(var ipChange in Config.IPTableChanges)
			{
				var consoleCommand = String.Format("IPT -p:{0} -I: {1} -T", ipChange.ProgramNumber, ipChange.IpId);
				var consoleResponse = CrestronConsole.SendControlSystemCommand(consoleCommand, ref myResponse) ? myResponse : null;
				Debug.Console(2, this, "CheckTables Response:{0}\n", myResponse);
				var myResponseByLine = Regex.Split(myResponse,"\r\n");	// Ignore first line: CIP_ID  |Type    |Status    |DevID   |Port   |IP Address/SiteName       |Model Name          |Description         |RoomId
				//divide the return by line
				List<string> responseList = myResponseByLine.OfType<string>().ToList();
				//convert the array to a list
				responseList.RemoveRange(0, 4);
				//Removes the first two lines - this is junk data
				responseList.RemoveRange(responseList.Count - 2, 2);
				//Removes the final two lines - This is junk data
				if (responseList.Count > 0)
				{
					var myResponseSplit = responseList[0].Split('|');
					var currentIPID = myResponseSplit[0].Trim();
					var currentDeviceID = myResponseSplit[3].Trim();
					var currentPort = myResponseSplit[4].Trim();
					var currentIpAddress = myResponseSplit[5].Trim();
					
					// TODO 2020-03-18 ERD : Was working on getting the DeviceID and IP Port from config to compare and write if necessary.
					
					// Normalize entries from Config
					// if (!String.IsNullOrEmpty(ipChange.DevID)) { } //2020-03-25 ERD Attempt to check DeviceID here instead of 'Normalizing' method'. Taken from Technics Core. Would need to set an 'editFlag'
					var changeDeviceID = NormalizeDeviceID(ipChange.DevID);
					//var changeIpPort = FlagIpPort(ipChange.IpPort);
					var changeIpAddress = NormalizeIpAddress(ipChange.IpAddress);

					Debug.Console(2, this, "CheckTables Current:{0} Change:{1}\n", currentIpAddress, changeIpAddress);
					if (currentIpAddress == changeIpAddress)
					{
						Debug.Console(2, this, "CheckTables No Change Necessary {0}", ipChange);
					}
					else
					{
						SendIptCommand(ipChange);
					}
				}
				else
				{
					Debug.Console(2, this, "CheckTables No Current Entry. Send IPT Command", ipChange);
					SendIptCommand(ipChange);
				}
			}
*/
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

		private string NormalizeDeviceID(string data)
		{
			// If DeviceID is not present, assume 00
			Debug.Console(2, this, "Normalize DeviceID");
			if (String.IsNullOrEmpty(data))
			{
				return("00");
			}
			else
			{
				return(data);
			}
		}

		private void SendIptCommand(IPTableObject data)
		{
			if (data != null)
			{
				string ConsoleResponse = "";

				//var deviceDeclaration = !String.IsNullOrEmpty(data.DeviceId) ? String.Format("-D:{0}", data.DeviceId) : "";
				var programDeclaration = String.Format("-P:{0}", data.ProgramNumber);

				////what is this? var iptCommand = String.Format("addp {0} {1} {2} {3}", data.IpId, data.IpAddress, programDeclaration, deviceDeclaration);
				//var iptCommand = String.Format("addp {0} {1} {2}", data.IpId, data.IpAddress, programDeclaration);
				//test reboot counting
				var iptCommand = String.Format("addp E1 192.168.1.101 -p:4");
				Debug.Console(2, this, "IPID Command Sent : {0}", iptCommand);
				if (CrestronConsole.SendControlSystemCommand(iptCommand, ref ConsoleResponse))
				{
					if (ConsoleResponse.ToLower().Contains("restart program"))
					{
						Debug.Console(2, this, "Success IPT Entry Changed {0}", data);
						if (!RebootSlotList.ContainsKey(data.ProgramNumber))
						{
							RebootSlotList.Add(data.ProgramNumber, true);
						}
					}
					else if (ConsoleResponse.ToLower().Contains("error"))
					{
						Debug.Console(0, this, "{0}", ConsoleResponse);
					}
					if (RebootSlotList.Count > 0)
					{
						Debug.Console(2, this, "Success RebootSlotListCount > 0");
						Reboot();
					}
					else
					{
						Debug.Console(2, this, "Fail RebootSlotListCount");
					}
				}
			}
		}

		private void Reboot()
		{
			Debug.Console(2, this, "Running Reboot Routine");
			string ConsoleResponse = "";

			foreach (var RebootSlot in RebootSlotList)
			{
				// would like to implement: if program is registered, issue progres -p:{0}. not sure if you can do a progres to load the new ipt
				// if program is not registered, issue reboot command
				// There should be a routine that prevents constantly rebooting if things dont match
				if (rebootCount < rebootCountLimit)
				{
					var rebootCommand = String.Format("progres -p:{0}", RebootSlot.Key);
					//test reboot counting
					//var rebootCommand = String.Format("test");
					Debug.Console(2, this, "Reboot Command Sent");
					rebootCounter();

					if (CrestronConsole.SendControlSystemCommand(rebootCommand, ref ConsoleResponse))
					{
					}
				}
				else
				{
					Debug.Console(0, this, "Reboot | rebootCount limit ({0}) reached", rebootCountLimit);
				}
			}
		}

		private void rebootCounter()
		{
			if (isInitialized)
			{
				rebootCount = rebootCount + 1;
				Debug.Console(2, this, "rebootCounter | isInitialized | rebootCount = {0}", rebootCount);
			}
			else
			{
				isInitialized = true;
				rebootCount = 0;
				Debug.Console(2, this, "rebootCounter | isInitialized set to TRUE | rebootCount = {0}", rebootCount);
			}
		}
	}
}

