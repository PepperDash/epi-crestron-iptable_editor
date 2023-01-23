// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Diagnostics;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace IPTableEditorPlugin 
{
	public class IpTableEditor : EssentialsBridgeableDevice
	{

		string _myResponse;
	    readonly IpTableEditorConfigObject _config;
        private readonly ReadOnlyDictionary<int, IpTableObjectBase> _mutableIpTableObjects;
	    public Dictionary<int, bool> IpTableObjectActive { get; set; }
        private readonly IpTableObjectBase _persistentIpTableObject;
        public IntFeedback IntSelectedFeedback { get; set; }

        private int IntFeedbackBacker { get; set; }

	    public IBasicCommunication Comm;
	    public ISocketStatus SocketStatus;
	    public CommunicationGather PortGather;
		//Dictionary<int, List<IPTableConfigObject>> SortedMods = new Dictionary<int, List<IPTableConfigObject>>();
		//public Dictionary<int, bool> HasMods;
		//public Dictionary<int, BoolFeedback> HasModsFeedback;
		//public Dictionary<int, bool> NeedsCheckTables;
		public Dictionary<int, ProgramSlot> ProgramSlots { get; private set; }

        private CrestronQueue Queue { get; set; }

	    //private CTimer _connectionTimer;

        private const string Delimiter =  "\x0D\x0A";

	    private int _selecting = 0;

        public FeedbackCollection<Feedback> Feedbacks { get; set; }

        private List<IpTableObjectBase> CurrentEntries { get; set; } 

		public IpTableEditor(string key, string name, DeviceConfig dc)
			: base(key, name)
		{
            var config = JsonConvert.DeserializeObject<IpTableEditorConfigObject>(dc.Properties.ToString());

			_config = config;
			ProgramSlots = new Dictionary<int, ProgramSlot>();
	
			SortMods();
			SystemMonitor.ProgramChange += SystemMonitor_ProgramChange;
		    if (!config.RunAtStartup) return;
		    for (var i = 1; i < 10; i++)
		    {
		        var localI = i;
		        CheckTableTrigger(localI);
		    }
		}

        public IpTableEditor(string key, string name, DeviceConfig dc, IBasicCommunication comm)
            : base(key, name)
        {

            Debug.Console(0, this, "Constructor with Comm!");
            var config = JsonConvert.DeserializeObject<IpTableEditorConfigObject>(dc.Properties.ToString());

            _config = config;

            Queue = new CrestronQueue(100);

            Comm = comm;
            SocketStatus = Comm as ISocketStatus;
            if (SocketStatus != null)
            {
                SocketStatus.ConnectionChange += SocketStatus_ConnectionChange;
            }

            IntSelectedFeedback = new IntFeedback(() => IntFeedbackBacker);

            _mutableIpTableObjects = new ReadOnlyDictionary<int, IpTableObjectBase>(_config.SelectableEntries);
            CurrentEntries = new List<IpTableObjectBase>();
            _persistentIpTableObject = _config.PersistentEntry;

            PortGather = new CommunicationGather(Comm, (Delimiter + Delimiter));
            PortGather.LineReceived += PortGather_LineReceived;
            Feedbacks = new FeedbackCollection<Feedback>();
            IpTableObjectActive = new Dictionary<int, bool>();
            foreach (var item in _mutableIpTableObjects)
            {
                var i = item;
                IpTableObjectActive.Add(i.Key, false);
                Feedbacks.Add((new BoolFeedback(i.Value.IpId, () => IpTableObjectActive[i.Key])));
            }
            if (_persistentIpTableObject != null)
                AddPersistentEntry();
            else
            {
                PollIpTable();
            }
        }

        void PortGather_LineReceived(object sender, GenericCommMethodReceiveTextArgs e)
        {
            Debug.Console(0, this, "Data Received on port : {0}", e.Text);

            var data = e.Text.Trim();
            if (data.ToLower().Contains("ip table")) ProcessIpTable(data);
            if(!Queue.IsEmpty)
                DequeueCmd(Queue);
        }

	    void ProcessIpTable(string data)
	    {
            Debug.Console(0, this, "Process IP Table");
	        var lines = Regex.Split(data, (Delimiter));
	        if (lines.Length <= 0) return;
	        var currentTable = new List<IpTableObjectBase>();
            foreach (var line in lines)
            {
                if (!line.Contains("|")) continue;
                if (!line.ToLower().Contains("cip_id"))
                {
	                Debug.Console(0, this, "Parsing Line : {0}", line);
                    var chunks = line.Split('|');
                    currentTable.Add(new IpTableObjectBase
                    {
                        IpAddress = !String.IsNullOrEmpty(chunks[5]) ? chunks[5].Trim() : "",
                        IpId = !String.IsNullOrEmpty(chunks[0]) ? chunks[0].Trim() : "",
                        RoomId = !String.IsNullOrEmpty(chunks[8]) ? chunks[8].Trim() : ""
                    });
                }
            }
            Debug.Console(0, this, "There are {0} Entries in the ip table", currentTable.Count);
            CurrentEntries = currentTable;
            Debug.Console(0, this, "CurrentEntries");

            foreach (var item in CurrentEntries)
            {
                Debug.Console(0, this, "Ipid : {0} | IpAddress : {1} | RoomId : {2}", item.IpId, item.IpAddress, item.RoomId ?? "N/A");
            }
            CompareEntries();

	    }

        private void CompareEntries()
        {
            Debug.Console(0, this, "CompareEntries");

            var tempDict = new Dictionary<int, bool>();

            foreach (var item in _mutableIpTableObjects)
            {
                var i = item;
                var linkedItem = CurrentEntries.FirstOrDefault(o => o.IpId == i.Value.IpId);
                var present = linkedItem != null;
                Debug.Console(0, this,"Feedback Entry {0} is {1}", i.Key, present);
                tempDict.Add(i.Key, linkedItem != null);
                if (present) IntFeedbackBacker = i.Key;
            }
            IpTableObjectActive = tempDict;
            UpdateFeedbacks();
        }

        private void UpdateFeedbacks()
        {
            foreach (var feedback in Feedbacks)
            {
                var f = feedback;
                f.FireUpdate();
                Debug.Console(0, this, "Feedback {0} = {1}", f.Key, f.BoolValue);
            }
            IntSelectedFeedback.FireUpdate();

        }

        void SocketStatus_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            Debug.Console(0, this, "ConnectionChange = {0}", e.Client.IsConnected ? "Connected" : "Disconnected");

            //_connectionTimer = null;
            if (!e.Client.IsConnected) return;
            //_connectionTimer = new CTimer(o => Comm.Disconnect(), 15000);
            Debug.Console(0, this, "Selecting = {0}", _selecting);
            if (_selecting != 0)
            {
                SwapEntry(_selecting);
            }
            if (Queue.IsEmpty) return;
            Debug.Console(0, this, "Queue is not empty - has {0} elements", Queue.Count);
            DequeueCmd(Queue);
        }

	    public void SelectEntry(int index)
	    {
            Debug.Console(0, this, "Select Entry = {0}", index);

	        if (!SocketStatus.IsConnected)
	        {
                _selecting = index;
	            Comm.Connect();
	        }
	        else
	        {
	            _selecting = index;
                SwapEntry(index);
	        }
	    }

	    public void SwapEntry(int index)
	    {
	        var removalList = CurrentEntries.Where(entry => entry.IpId != _persistentIpTableObject.IpId && entry.IpId != _mutableIpTableObjects[index].IpId).ToList();

            AddEntry(_mutableIpTableObjects[index], true);
            RemoveMultipleEntries(removalList, false);

	    }

	    private void ClearAndAdd(int index)
	    {
            _selecting = 0;
	        IpTableObjectBase newEntry;
	        _mutableIpTableObjects.TryGetValue(index, out newEntry);
	        if (newEntry == null)
	        {
	            Debug.Console(0, this, "Invalid Entry {0} Selected", index);
	            return;
	        }
            Debug.Console(0, this, "Clear and add {0}", index);
            ClearTable();
            AddMultipleEntries(new List<IpTableObjectBase>()
            {
                _persistentIpTableObject,
                _mutableIpTableObjects[index]
            }, true);
	    }

	    public void PollIpTable()
	    {
            EnqueueCmd("ipt -t", Queue);
	    }

	    private void RemoveEntry(IpTableObjectBase entry, bool suppressPoll)
	    {
	        var cmd = String.Format("remm {0} {1} {2}", entry.IpId, entry.IpAddress, entry.RoomId);
	        EnqueueCmd(cmd, Queue);
	        if (suppressPoll) return;
            PollIpTable();
	    }

	    private void AddEntry(IpTableObjectBase entry, bool suppressPoll)
	    {
	        var cmd = String.Format("addm {0} {1} {2}", entry.IpId, entry.IpAddress, entry.RoomId);
	        EnqueueCmd(cmd, Queue);
	        if (suppressPoll) return;
            PollIpTable();
	    }

        private void RemoveMultipleEntries(IEnumerable<IpTableObjectBase> entries, bool suppressPoll)
        {
            foreach (var cmd in entries.Select(entry => String.Format("remm {0} {1} {2}", entry.IpId, entry.IpAddress, entry.RoomId)))
            {
                EnqueueCmd(cmd, Queue);
            }
            if (suppressPoll) return;
            PollIpTable();
        }



        private void AddMultipleEntries(IEnumerable<IpTableObjectBase> entries, bool suppressPoll)
	    {
	        foreach (var cmd in entries.Select(entry => String.Format("addm {0} {1} {2}", entry.IpId, entry.IpAddress, entry.RoomId)))
	        {
	            EnqueueCmd(cmd, Queue);
	        }
            if (suppressPoll) return;
	        PollIpTable();
	    }

	    private void AddPersistentEntry()
	    {
	        AddEntry(_persistentIpTableObject, false);
	    }

	    private void ClearTable()
	    {
            EnqueueCmd("ipt -c", Queue);
	    }

	    private void EnqueueCmd(string cmd, CrestronQueue queue)
	    {
            Debug.Console(0, this, "Enqueued Cmd : {0}", cmd);
	        queue.Enqueue(cmd);
	        if (!SocketStatus.IsConnected)
	        {
                Debug.Console(0, this, "Command Enqueued - Connecting Socket!");
	            Comm.Connect();
	            return;
	        }
            Debug.Console(0, this, "Command Enqueued and Socket Already Connected!");
	        CheckQueue(queue);
	    }

	    private void CheckQueue(CrestronQueue queue)
	    {
	        if (!queue.IsEmpty)
	        {
	            DequeueCmd(queue);
	        }
	    }

	    private void DequeueCmd(CrestronQueue queue)
	    {
            Debug.Console(0, this, "Dequeueing Command");
	        var cmd = queue.Dequeue() as string;
            Debug.Console(0, this, "Command is : {0}", cmd);
            SendCmd(cmd);
	    }

	    public void SendCmd(string data)
	    {

	        var cmd = string.Format("{0}{1}", data, Delimiter);
	        Comm.SendText(cmd);
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
		    if (Comm == null)
		    {
                var joinMap = new IpTableEditorBridgeJoinMap(joinStart);
                if (bridge != null)
                    bridge.AddJoinMap(Key, joinMap);

                var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
                if (customJoins != null)
                    joinMap.SetCustomJoinData(customJoins);

		        Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
		        Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);

		        for (int i = 0; i < 10; i++)
		        {
		            var join = (uint) (joinMap.CheckTable.JoinNumber + i);
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
		    else
		    {

                var joinMap = new IpTableSelectorBridgeJoinMap(joinStart, _mutableIpTableObjects.Count);
                if (bridge != null)
                    bridge.AddJoinMap(Key, joinMap);

                var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
                if (customJoins != null)
                    joinMap.SetCustomJoinData(customJoins);

		        foreach (var item in _mutableIpTableObjects)
		        {
		            var i = item;
		            trilist.SetSigTrueAction((UInt16)(joinMap.SelectItemBool.JoinNumber + i.Key - 1), () => SelectEntry(i.Key));

		            var fb = Feedbacks[i.Value.IpId] as BoolFeedback;
		            if (fb == null) continue;
                    fb.LinkInputSig(trilist.BooleanInput[(uint)(joinMap.SelectItemBool.JoinNumber + i.Key - 1)]);
		        }

		        trilist.SetUShortSigAction(joinMap.SelectItemAnalog.JoinNumber, (a) => SelectEntry(a));
                IntSelectedFeedback.LinkInputSig(trilist.UShortInput[joinMap.SelectItemAnalog.JoinNumber]);


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

