﻿using System;
using System.Text.RegularExpressions;
using System.Linq;

using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.Diagnostics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

using PepperDash.Essentials;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;

namespace IPTableEditorEPI 
{
	public class IPTableEditor : Device, IBridge
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
		IPTableEditorConfigObject Config;
		Dictionary<int, bool> RebootSlotList;
		//Dictionary<int, List<IPTableObject>> SortedMods = new Dictionary<int, List<IPTableObject>>();
		//public Dictionary<int, bool> HasMods;
		//public Dictionary<int, BoolFeedback> HasModsFeedback;
		//public Dictionary<int, bool> NeedsCheckTables;
		public Dictionary<int, ProgramSlot> ProgramSlots { get; private set; }

		public IPTableEditor(string key, string name, IPTableEditorConfigObject config)
			: base(key, name)
		{
			Config = config;
			RebootSlotList = new Dictionary<int, bool>();
			ProgramSlots = new Dictionary<int, ProgramSlot>();
	
			SortMods();
			SystemMonitor.ProgramChange += new ProgramStateChangeEventHandler(SystemMonitor_ProgramChange);
		}

		void SystemMonitor_ProgramChange(Program sender, ProgramEventArgs args)
		{
			if (args.EventType == eProgramChangeEventType.OperatingState)
			{
				if (args.OperatingState == eProgramOperatingState.Start)
				{
					var startedProgram = (int)args.ProgramNumber;
					if (ProgramSlots[startedProgram].NeedsCheckTables == true)
					{
						CheckTables(startedProgram);
					}
				}
			}
		}
	
		private void SortMods()
		{

			ProgramSlots.Clear();
			for (int i = 1; i < 11; i++) 
			{
				var slot = i;
				var programSlot = new ProgramSlot();
				ProgramSlots.Add(slot, programSlot);
				programSlot.HasMods = false;
				programSlot.HasModsFeedback =  new BoolFeedback(() => 
					{
						Debug.Console(2, this, "the value of i is: {0} in HasModsFeedbackFunc", slot);
						if (ProgramSlots.ContainsKey(slot))
							return programSlot.HasMods;
						else
						{
							Debug.Console(2, this, "Unable to find key '{0}' in HasMods", slot);
							return false;
						}

					});
				var LocalMods = new List<IPTableObject>();
				List<IPTableObject> selected = Config.IPTableChanges.Where(item => item.ProgramNumber == slot).ToList();
				if (selected != null && selected.Count() > 0)
				{					
					Config.IPTableChanges = Config.IPTableChanges.Except(selected).ToList();
					LocalMods.AddRange(selected);
					Debug.Console(2, this, "SortMods | Adding {0} mods to slot {1}", LocalMods.Count, slot);
					programSlot.SortedMods = LocalMods;
					programSlot.HasMods = programSlot.SortedMods.Count > 0 ? true : false;
					programSlot.HasModsFeedback.FireUpdate();
				}
				else
				{
					Debug.Console(2, this, "SortMods | Slot {0} had no mods", slot);
				}
			}
		}

		public void CheckTables(int slot)
		{
			if (slot < 1 || slot > 10)
				return;

			ProgramSlots[slot].NeedsCheckTables = true;
			List<string> commandList = new List<string>();
			var localIPTableObject = ProgramSlots[slot].SortedMods;
			Debug.Console(2, this, "CheckTables | Checking Slot:{0}", slot);
			if (localIPTableObject != null)
			{
				foreach (var ipChange in localIPTableObject)
				{
					var consoleCommand = String.Format("IPT -p:{0} -I: {1} -T", ipChange.ProgramNumber, ipChange.IpId);
					var consoleResponse = CrestronConsole.SendControlSystemCommand(consoleCommand, ref myResponse) ? myResponse : null;
					Debug.Console(2, this, "CheckTables | Response:{0}\n", myResponse);
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

						// Normalize entries from Config
						var changeIpAddress = NormalizeIpAddress(ipChange.IpAddress);

						Debug.Console(2, this, "CheckTables | IPID:{0} :: Current IPA:{1} :: Requested IPA:{2}", ipChange.IpId, currentIpAddress, changeIpAddress);
						if (currentIpAddress == changeIpAddress)
						{
							Debug.Console(2, this, "CheckTables | No Change Necessary for IPID:{0} on Slot:{0}", ipChange.IpId, ipChange.ProgramNumber);							
						}
						else
						{
							var cmd = BuildIptCommand(ipChange);
							if (!string.IsNullOrEmpty(cmd))
							{
								commandList.Add(cmd);
							}
						}
					}
					else
					{
						Debug.Console(2, this, "CheckTables | No Current Entry for IPID:{0} on Slot:{0}. Send IPT Command", ipChange.IpId, ipChange.ProgramNumber);
						var cmd = BuildIptCommand(ipChange);
						if (!string.IsNullOrEmpty(cmd))
						{
							commandList.Add(cmd);
						}
					}
				}
				if (commandList.Count > 0)
				{
					commandList.Add(string.Format("progres -p:{0}", slot));
					SendCommandList(commandList, slot);
				}
				else if (commandList.Count == 0)
				{
					Debug.Console(2, this, "Setting Hasmods {0} to false", slot);

					if (ProgramSlots.ContainsKey(slot))
						ProgramSlots[slot].HasMods = false;
					else
						Debug.Console(2, this, "No '{0}' Key found in HasMods", slot);

					Debug.Console(2, this, "Firing HasModsFeedback {0}", slot);

					if (ProgramSlots.ContainsKey(slot))
						ProgramSlots[slot].HasModsFeedback.FireUpdate();
					else
						Debug.Console(2, this, "No '{0}' Key found in HasModsFeedback", slot);

					Debug.Console(2, this, "Fired HasMods Feedback {0}", slot);
				}
			}
		}

		private string NormalizeIpAddress(string data)
		{
			//Normalizes all ip addresses to utilize three digits in each octet
			//Passes hostnames directly out without manipulation
			Debug.Console(2, this, "NormalizeIpAddress | Input = {0}", data);
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
						Debug.Console(2, this, "NormalizeIpAddress | Return = {0}", data);
						return data;
					}
					if (myOctets[i].Length > 3)
					{
						//If an index has more than three digits, it's a hostname
						Debug.Console(2, this, "NormalizeIpAddress | Return = {0}", data);
						return data;
					}
					else
						//make sure each index has at least 3 digits - may break some hostnames
						myOctets[i] = myOctets[i].PadLeft(3, '0');
				}
				var myReturn = string.Join(".", myOctets.ToArray());
				Debug.Console(2, this, "NormalizeIpAddress | Return = {0}", myReturn);
				return myReturn;
			}
			else
				Debug.Console(2, this, "NormalizeIpAddress | Return = {0}", data);
			return data;
		}

		private string BuildIptCommand(IPTableObject data)
		{
			if (data != null)
			{
				var programDeclaration = string.Format("-P:{0}", data.ProgramNumber);
				var iptCommand = string.Format("addp {0} {1} {2}", data.IpId, data.IpAddress, programDeclaration);
				return iptCommand;
			}
			else
			{
				return string.Empty;
			}
		}

		private void SendCommandList(List<string> data, int ProgramNumber)
		{
			foreach (string iptCommand in data)
			{
				string ConsoleResponse = string.Empty;
				Debug.Console(2, this, "SendCommandList | IPID Command Sent : {0}", iptCommand);
				if (CrestronConsole.SendControlSystemCommand(iptCommand, ref ConsoleResponse))
				{
					if (ConsoleResponse.ToLower().Contains("error"))
					{
						Debug.Console(0, this, "SendCommandList | Fail! {0}", ConsoleResponse);
					}
				}
			}
		}

		public void CheckTableTrigger(int slot)
		{
			ProgramSlots[slot].NeedsCheckTables = true;
			CheckTables(slot);
		}

		#region IBridge Members

		public void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			this.LinkToApiExt(trilist, joinStart, joinMapKey);
		}

		#endregion
	}

	public class ProgramSlot
	{
		public List<IPTableObject> SortedMods { get; set; }
		public bool HasMods { get;  set; }
		public BoolFeedback HasModsFeedback { get; set; }
		public bool NeedsCheckTables { get; set; }

		public ProgramSlot()
		{
			SortedMods = new List<IPTableObject>();
			NeedsCheckTables = false;
		}
	}
}
