// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Diagnostics;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;

namespace IPTableEditorPlugin 
{
	public class IpTableEditor : EssentialsBridgeableDevice
	{

		string _myResponse;
	    readonly IpTableEditorConfigObject _config;
		Dictionary<int, bool> _rebootSlotList;
		//Dictionary<int, List<IPTableConfigObject>> SortedMods = new Dictionary<int, List<IPTableConfigObject>>();
		//public Dictionary<int, bool> HasMods;
		//public Dictionary<int, BoolFeedback> HasModsFeedback;
		//public Dictionary<int, bool> NeedsCheckTables;
		public Dictionary<int, ProgramSlot> ProgramSlots { get; private set; }

		public IpTableEditor(string key, string name, DeviceConfig dc)
			: base(key, name)
		{
            var config = JsonConvert.DeserializeObject<IpTableEditorConfigObject>(dc.Properties.ToString());

			_config = config;
			_rebootSlotList = new Dictionary<int, bool>();
			ProgramSlots = new Dictionary<int, ProgramSlot>();
	
			SortMods();
			SystemMonitor.ProgramChange += new ProgramStateChangeEventHandler(SystemMonitor_ProgramChange);
		    if (!config.RunAtStartup) return;
		    for (var i = 1; i < 10; i++)
		    {
		        var localI = i;
		        CheckTableTrigger(localI);
		    }
		}

		void SystemMonitor_ProgramChange(Program sender, ProgramEventArgs args)
		{
		    if (args.EventType != eProgramChangeEventType.OperatingState) return;
		    if (args.OperatingState != eProgramOperatingState.Start) return;
		    var startedProgram = (int)args.ProgramNumber;
		    if (ProgramSlots[startedProgram].NeedsCheckTables == true)
		    {
		        CheckTables(startedProgram);
		    }
		}

	    private void SortMods()
		{

			ProgramSlots.Clear();
			for (var i = 1; i < 11; i++) 
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
				var localMods = new List<IpTableChangesConfigObject>();
				var selected = _config.IpTableChanges.Where(item => item.ProgramNumber == slot).ToList();
				if (selected != null && selected.Any())
				{					
					_config.IpTableChanges = _config.IpTableChanges.Except(selected).ToList();
					localMods.AddRange(selected);
					Debug.Console(2, this, "SortMods | Adding {0} mods to slot {1}", localMods.Count, slot);
					programSlot.SortedMods = localMods;
					programSlot.HasMods = programSlot.SortedMods.Count > 0 ? true : false;
					programSlot.HasModsFeedback.FireUpdate();
				}
				else
				{
					Debug.Console(2, this, "SortMods | Slot {0} had no mods", slot);
				}
			}
		}

		/// <summary>
		/// Check tables
		/// </summary>
		/// <param name="slot">Integer value representing the program slot</param>
		public void CheckTables(int slot)
		{
			if (slot < 1 || slot > 10)
				return;

			ProgramSlots[slot].NeedsCheckTables = true;
			var commandList = new List<string>();
			var localIpTableObject = ProgramSlots[slot].SortedMods;
			Debug.Console(2, this, "CheckTables | Checking Slot:{0}", slot);
		    if (localIpTableObject == null) return;
		    foreach (var ipChange in localIpTableObject)
		    {
		        var consoleCommand = String.Format("IPT -p:{0} -I: {1} -T", ipChange.ProgramNumber, ipChange.IpId);
		        var consoleResponse = CrestronConsole.SendControlSystemCommand(consoleCommand, ref _myResponse) ? _myResponse : null;
		        Debug.Console(2, this, "CheckTables | Response:{0}\n", _myResponse);
		        var myResponseByLine = Regex.Split(_myResponse, "\r\n");	// Ignore first line: CIP_ID  |Type    |Status    |DevID   |Port   |IP Address/SiteName       |Model Name          |Description         |RoomId
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
		            var currentIpid = myResponseSplit[0].Trim();
		            var currentDeviceId = myResponseSplit[3].Trim();
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

		private string NormalizeIpAddress(string data)
		{
			//Normalizes all ip addresses to utilize three digits in each octet
			//Passes hostnames directly out without manipulation
			Debug.Console(2, this, "NormalizeIpAddress | Input = {0}", data);
			//remove "(not resolved)" from unresolved hostnames in the IPTable entry list.
			data = Regex.Replace(data, @"\(([^)]*)\)$", "");

			string[] myOctetsArray = data.Split('.');
			var myOctets = myOctetsArray.OfType<string>().ToList();
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

		private string BuildIptCommand(IpTableChangesConfigObject data)
		{
			if (data == null) return string.Empty;
			
			var programDeclaration = string.Format("-P:{0}", data.ProgramNumber);
			var iptCommand = string.Format("addp {0} {1} {2}", data.IpId, data.IpAddress, programDeclaration);
			return iptCommand;			
		}

		private void SendCommandList(List<string> data, int programNumber)
		{
			foreach (var iptCommand in data)
			{
				var consoleResponse = string.Empty;
				Debug.Console(2, this, "SendCommandList | IPID Command Sent : {0}", iptCommand);
				if (!CrestronConsole.SendControlSystemCommand(iptCommand, ref consoleResponse)) continue;
				if (consoleResponse.ToLower().Contains("error"))
				{
					Debug.Console(0, this, "SendCommandList | Fail! {0}", consoleResponse);
				}
			}
		}

		/// <summary>
		/// Check Table Trigger
		/// </summary>
		/// <param name="slot"></param>
		public void CheckTableTrigger(int slot)
		{
			ProgramSlots[slot].NeedsCheckTables = true;
			CheckTables(slot);
		}

		/// <summary>
		/// Links the plugin device to the EISC bridge
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new IpTableEditorBridgeJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

            var joinMapSerialized = JoinMapHelper.GetJoinMapForDevice(joinMapKey);

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);

            for (int i = 0; i < 10; i++)
            {
                var join = (uint)(joinMap.CheckTable.JoinNumber + i);
                var slot = i + 1;
                Debug.Console(1, this, "Linking join {0} to slot {1}", join, slot);
                //trilist.BooleanInput[((ushort)(joinMap.CheckTable + ))].BoolValue = IptDevice.HasMods[i];
                var programSlot = ProgramSlots[slot];
                if (programSlot == null) continue;
                programSlot.HasModsFeedback.LinkInputSig(trilist.BooleanInput[@join]);
                trilist.SetSigTrueAction(@join, () =>
                {
                    Debug.Console(1, this, "Attempting to CheckTables for slot {0}", slot);
                    CheckTableTrigger(slot);
                });
            }
        }
    }

	/// <summary>
	/// Program Slot
	/// </summary>
	public class ProgramSlot
	{
		public List<IpTableChangesConfigObject> SortedMods { get; set; }
		public bool HasMods { get;  set; }
		public BoolFeedback HasModsFeedback { get; set; }
		public bool NeedsCheckTables { get; set; }

		public ProgramSlot()
		{
			SortedMods = new List<IpTableChangesConfigObject>();
			NeedsCheckTables = false;
		}
	}
}

