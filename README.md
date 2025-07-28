![PepperDash Logo](/images/essentials-plugin-blue.png)

# PepperDash Essentials TCP/IP Table Editor

This repo contains a plugin for use with [PepperDash Essentials](https://github.com/PepperDash/Essentials).

## License

Provided under MIT license

## Overview

The **IP Table Editor Plugin** is a PepperDash Essentials Plugin (EPI) that enables dynamic runtime editing and management of Crestron IP Table entries for devices such as touchpanels, XPanels, and network clients. This plugin operates in **two distinct modes** based on the presence of communication configuration:

### **Editor Mode** (configuration `control` object not included)
- **Purpose**: Manages IP table entries on the **local control processor** where the plugin is running
- **Use Case**: Direct control of the host processor's IP table for local device management
- **Configuration**: Uses `ipTableChanges` array without `control` object, see example below

***Important*** Plugin `Editor Mode` modifies existing IP table entries only. All IP-IDs must be pre-defined in SIMPL Windows with the `remap` option enabled. Plugin cannot create new IP-IDs or change IP-ID assignments.

![Screenshot](/images/IP-ID-Remap.png)

### **Selector Mode** (configuration `control` object included)
- **Purpose**: Manages IP table entries on a **remote device** via SSH/TCP communication
- **Use Case**: Host control processor remotely managing `master` IP table entry of touchpanel, DM-NVX, or similar
- **Configuration**: Uses **either** `persistentEntry` **or** `selectableEntries` (typically not both due to single master entry limitation)

## Features

### **Editor Mode Features**
- **Local IP Table Management:** Modify IP table entries on the host control processor
- **Program Slot Control:** Trigger IP table checks and updates for specific program slots (1-10)
- **Batch Operations:** Apply multiple IP table changes via `ipTableChanges` configuration
- **Startup Automation:** Optionally applies changes automatically at startup

### **Selector Mode Features**
- **Remote IP Table Management:** Control IP tables on remote devices via SSH/TCP communication
- **Dynamic Selection:** Choose between predefined IP table configurations (when using `selectableEntries`)
- **Persistent Entries:** Maintain single always-selected IP table entry (when using `persistentEntry`) triggered at construction
- **Real-time Feedback:** Monitor remote device IP table status and changes
- **⚠️ Configuration:** Use **either** `persistentEntry` **or** `selectableEntries`, typically not both

### **Common Features**
- **SIMPL Windows Bridge Integration:** Exposes join map for control and feedback via Essentials Device Bridge (EISC)
- **Diagnostics:** Feedback joins for table status and change operations
- **Runtime Operation:** No program reloads or SIMPL Windows recompilation required

## Use Cases

### **Editor Mode**
- Local IP table management on the host control processor
- Commissioning scenarios where local device assignments need updates
- Batch IP table changes during system startup or configuration
- Direct control of the processor's own IP table entries

### **Selector Mode**  
- One control processor managing remote IP table entry of remote device (touchpanel, DM-NVX, or similar)
- Hot-swappable device scenarios (e.g., backup touchpanels with different IP assignments)
- Centralized IP table management across multiple devices
- Remote commissioning and service operations
- Systems requiring API-driven or user-selectable IP configurations

## SIMPL EISC Bridge Map
The bridge join map **varies depending on the operational mode** (Editor vs Selector). The plugin automatically selects the appropriate join map based on the presence of the `control` object in configuration.

### **Editor Mode Bridge Map** (No Communication - `control` object not present)
Uses `IpTableEditorBridgeJoinMap` for program slot control:

#### Digitals
| dig-o (Input/Triggers)     | I/O   | dig-i (Feedback)     |
|----------------------------|-------|----------------------|
| CheckTable Program Slot 1 | 1     | UpdateNeeded Slot 1  |
| CheckTable Program Slot 2 | 2     | UpdateNeeded Slot 2  |
| CheckTable Program Slot 3 | 3     | UpdateNeeded Slot 3  |
| CheckTable Program Slot 4 | 4     | UpdateNeeded Slot 4  |
| CheckTable Program Slot 5 | 5     | UpdateNeeded Slot 5  |
| CheckTable Program Slot 6 | 6     | UpdateNeeded Slot 6  |
| CheckTable Program Slot 7 | 7     | UpdateNeeded Slot 7  |
| CheckTable Program Slot 8 | 8     | UpdateNeeded Slot 8  |
| CheckTable Program Slot 9 | 9     | UpdateNeeded Slot 9  |
| CheckTable Program Slot 10| 10    | UpdateNeeded Slot 10 |

#### Analogs
| an_o (Input/Triggers) | I/O | an_i (Feedback)  |
|-----------------------|-----|------------------|
| _Not Used_            | -   | _Not Used_       |

#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback)  |
|---------------------------|-----|----------------------|
| _Not Used_                | -   | _Not Used_           |

### **Selector Mode (Selectable Entries) Bridge Map** (With Communication - `control` object present)
Uses `IpTableSelectorBridgeJoinMap` for entry selection:

#### Digitals
| dig-o (Input/Triggers)    | I/O       | dig-i (Feedback)         |
|---------------------------|-----------|--------------------------|
| Select Entry 1            | 1         | Entry 1 Selected         |
| Select Entry 2            | 2         | Entry 2 Selected         |
| Select Entry 3            | 3         | Entry 3 Selected         |
| Select Entry N            | N         | Entry N Selected         |
| _(Span = # of entries)_   | _dynamic_ | _(Span = # of entries)_  |

#### Analogs
| an_o (Input/Triggers) | I/O | an_i (Feedback)      |
|-----------------------|-----|----------------------|
| Select Entry by Index | 1   | Selected Entry Index |

#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback)  |
|---------------------------|-----|----------------------|
| _Not Used_                | -   | _Not Used_           |

### **Selector Mode (Persistent Entry) Bridge Map** (With Communication - `control` object present)
No bridge required. IP table entry sent only once post plugin construction. 

## Configuration Examples

### **Editor Mode Configuration** (Local IP Table Management)
Used when managing IP table entries on the **local control processor**. No `control` object is required.

```json
{
    "key": "iptable-editor-local",
    "uid": 1,
    "name": "IP Table Editor - Local Mode",
    "type": "iptableeditor",
    "group": "utilities",
    "properties": {
        "runAtStartup": false,
        "ipTableChanges": [
            {
                "name": "TP01 - Main",
                "ipId": "03",
                "ipAddress": "192.168.1.100",
                "devId": "03",
                "programNumber": 1
            },
            {
                "name": "TP02 - Conference Room",
                "ipId": "04",
                "ipAddress": "192.168.1.101",
                "devId": "04", 
                "programNumber": 1
            },
            {
                "name": "TCP Client - AV Switcher",
                "ipId": "10",
                "ipAddress": "192.168.1.200",
                "ipPort": 23,
                "programNumber": 1
            }
        ]
    }
}
```

#### **`runAtStartup` Configuration Option**
The `runAtStartup` property controls automatic IP table updates in **Editor Mode only**:

- **`"runAtStartup": true`**: IP table changes are automatically applied **once** when the EPI initializes (during processor boot or Essentials app restart)
- **`"runAtStartup": false`**: IP table changes must be manually triggered via bridge joins or API calls

**Important Limitations:**
- ⚠️ **One-time operation**: `runAtStartup` only applies changes at EPI initialization - it does **not** monitor program slots for resets
- ⚠️ **Editor Mode only**: This option has no effect in Selector Mode (remote communication)
- ⚠️ **Manual monitoring**: If programs restart after EPI initialization, you must manually trigger IP table checks via bridge joins (1-10) or reset the program slot the EPI resides in

**Use Cases:**
- `true`: Fixed configurations that should be set once at system startup
- `false`: Dynamic configurations requiring manual/programmatic control

### **Selector Mode Configuration** (Remote IP Table Management)
Used when managing IP table entries on a **remote device** via communication. Requires `control` object and uses **either** `persistentEntry` **or** `selectableEntries`.

**⚠️ Configuration Rule:** Use **either** `persistentEntry` **or** `selectableEntries`, but **not both** due to single master IP table entry limitation on Crestron devices.

```json
{
    "key": "iptable-editor-remote",
    "uid": 2,
    "name": "IP Table Editor - Selector Mode", 
    "type": "iptableeditor",
    "group": "utilities",
    "properties": {
        "control": {
            "method": "ssh",
            "tcpSshProperties": {
                "address": "192.168.1.50",
                "port": 22,
                "username": "crestron",
                "password": "password"
            }
        },
        "persistentEntry": {
            "name": "Always Selected Entry",
            "ipId": "01",
            "ipAddress": "192.168.1.10",
            "roomId": "MainRoom"
        },
        "selectableEntries": {
            "1": {
                "name": "Primary Touchpanel",
                "ipId": "03",
                "ipAddress": "192.168.1.100",
                "roomId": "MainRoom"
            },
            "2": {
                "name": "Backup Touchpanel", 
                "ipId": "03",
                "ipAddress": "192.168.1.101",
                "roomId": "MainRoom"
            },
            "3": {
                "name": "Mobile Device Config",
                "ipId": "03",
                "ipAddress": "192.168.1.102",
                "roomId": "MainRoom"
            }
        }
    }
}
```

## Essentials Device Bridge Configuration
The bridge configuration is the same for both modes. 

**Note:** When `runAtStartup: true` is set in Editor Mode, the bridge is not required for the automatic startup functionality, but may still be useful for manual triggering of IP table checks.

```json
{
    "key": "essentials-device-bridge1",
    "uid": 3,
    "name": "Essentials Device Bridge",
    "group": "api",
    "type": "eiscApiAdvanced",
    "properties": {
        "control": {
            "tcpSshProperties": {
                "address": "127.0.0.1",
                "port": 0
            },
            "ipid": "A0",
            "method": "ipidTcp"
        },
        "devices": [
            {
                "deviceKey": "iptable-editor-local",
                "joinStart": 1
            }
        ]
    }
}
```

### Key Configuration Differences

| Aspect | Editor Mode | Selector Mode |
|--------|-------------|---------------|
| **Communication** | No `control` object | Requires `control` object |
| **IP Entries** | Uses `ipTableChanges` array | Uses `selectableEntries` dictionary |
| **Target Device** | Local processor | Remote device via SSH/TCP |
| **Program Slot** | Uses `programNumber` per entry | Uses `SelectEntry` index for operations |
| **Bridge Map** | `IpTableEditorBridgeJoinMap` | `IpTableSelectorBridgeJoinMap` |
| **Join Functionality** | Program slot triggers (1-10) | Entry selection (dynamic span) |
| **Persistent Entries** | Not supported | Optional via `persistentEntry` |
| **Selectable Entries** | Not supported | Supported via `selectableEntries` dictionary |
| **`runAtStartup`** | Applies changes once at EPI startup | No effect (selection required) |
| **Ongoing Monitoring** | Manual triggers only (no automatic monitoring) | Manual selection required |

## Operational Workflows

### **Editor Mode Workflow** (Local IP Table Management)

#### **Construction to Runtime Flow**
1. **Plugin Construction** (`IpTableEditor` without communication)
   - Loads `ipTableChanges` configuration array
   - Creates `ProgramSlots` dictionary for slots 1-10
   - Sets up bridge join map (`IpTableEditorBridgeJoinMap`)
   - If `runAtStartup: true` → immediately applies all IP table changes

2. **Runtime Operation** 
   - **Manual Trigger**: User activates bridge join 1-10 → `CheckTables(slot)` → `SendCommandList()`
   - **Command Execution**: `addp {ipId} {ipAddress} -P:{programNumber}` sent to local processor
   - **Feedback**: Updates `UpdateNeeded` feedback joins based on comparison results

#### **Command Details**
- **`addp` = "Add Peer"**: Adds peer IP table entries to local processor
- **Purpose**: Multiple peer entries allowed (processor ↔ touchpanel, third-party integrations)
- **Execution**: Via `CrestronConsole.SendControlSystemCommand()` (local)

### **Selector Mode Workflow** (Remote IP Table Management)

#### **Construction to Runtime Flow**
1. **Plugin Construction** (`IpTableEditor` with communication)
   - Loads `selectableEntries` dictionary and optional `persistentEntry`
   - Establishes SSH/TCP communication to remote device
   - Sets up bridge join map (`IpTableSelectorBridgeJoinMap`) with dynamic span
   - If `persistentEntry` exists → immediately sends `addm` command to remote device
   - Polls remote IP table state for initial feedback

2. **Runtime Operation**
   - **Manual Trigger**: User activates selection join → `SelectEntry(index)` → `ClearAndAdd(index)`
   - **Command Execution Sequence**:
     1. `ipt -p:{index} -c` (clear remote program slot)
     2. `addm {persistentIpId} {persistentIP} {persistentRoom}` (if configured)
     3. `addm {selectedIpId} {selectedIP} {selectedRoom}` (selected entry)
     4. `ipt -t` (poll for feedback)
   - **Feedback**: Updates selection feedback joins based on active IP table state

#### **Command Details**
- **`addm` = "Add Master"**: Adds master IP table entries to remote device
- **Purpose**: Single master entry per device (touchpanel or DM-NVX)
- **Execution**: Via SSH/TCP communication to remote device
- **⚠️ Critical Limitation**: Some Crestron devices allow for only a **single master entry**

#### **Example Execution Sequence**
User selects Entry 2 on remote touchpanel:
```
> ipt -p:02 -c                                    // Clear program slot 2
> addm 01 192.168.1.10 MainRoom                   // Add persistent entry (if configured)
> addm 03 192.168.1.101 MainRoom                  // Add selected entry #2  
> ipt -t                                          // Poll current table state
< IP Table: 01-192.168.1.10, 03-192.168.1.101    // Response for feedback
```

#### **Key Design Considerations**
- **Persistent vs Selectable**: Due to single master limitation, typically use **one or the other**:
  - **Persistent Entry**: For always-selected master connections (e.g., touch panel or DM-NVX)
  - **Selectable Entries**: For dynamic master selection (e.g., hot-swappable configurations)
- **Program Slot Targeting**: Uses selection index to determine which remote program slot to clear
- **Connection Management**: Handles SSH/TCP connectivity and command queuing automatically

## Public API Reference

### Classes
- **IpTableEditor**
  - Main plugin device class. Handles runtime IP table management, device selection, and communication.
  - **Public Properties:**
    - `IpTableObjectActive`: Dictionary of active IP table objects.
    - `IntSelectedFeedback`: Feedback for selected entry.
    - `Comm`: Communication object (if used).
    - `SocketStatus`: Socket status interface.
    - `PortGather`: Communication gather object.
    - `ProgramSlots`: Dictionary of program slots.
    - `Feedbacks`: Collection of feedback objects.
  - **Public Methods:**
    - `SelectEntry(int index)`: Selects an entry by index.
    - `SwapEntry(int index)`: Swaps the selected entry.
    - `PollIpTable()`: Polls the current IP table state.
    - `SendCmd(string data)`: Sends a command string to the device.
    - `CheckTables(int slot)`: Checks the table for a given slot.
    - `CheckTableTrigger(int slot)`: Triggers a table check for a slot.
    - `LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)`: Links the device to the Essentials API bridge.

- **ProgramSlot**
  - Represents a program slot for IP table entries.

- **IpTableEditorFactory**
  - Factory class for creating `IpTableEditor` plugin devices.
  - **Public Methods:**
    - `BuildDevice(DeviceConfig dc)`: Instantiates a new plugin device from configuration.

- **IpTableEditorBridgeJoinMap**
  - Defines the join map for the Essentials Device Bridge (EISC).
  - **Public Properties:**
    - `CheckTable`: Digital join for table check feedback and trigger.

- **IpTableSelectorBridgeJoinMap**
  - Defines join map for selectable IP table entries.
  - **Public Properties:**
    - `SelectItemBool`: Digital join array for selecting entries.
    - `SelectItemAnalog`: Analog join for selecting entries.

- **IpTableEditorConfigObject**
  - Configuration object for the plugin.
  - **Public Properties:**
    - `IpTableChanges`: List of IP table change objects.
    - `RunAtStartup`: Whether to apply changes at startup.
    - `Control`: Control properties for device communication.
    - `PersistentEntry`: Persistent IP table entry.
    - `SelectableEntries`: Dictionary of selectable entries.

- **IpTableObjectBase**
  - Base class for IP table entry objects.
  - **Public Properties:**
    - `IpId`, `IpAddress`, `RoomId`

- **IpTableChangesConfigObject**
  - Inherits from `IpTableObjectBase`. Represents a changeable IP table entry.
  - **Public Properties:**
    - `Name`, `DevId`, `IpPort`, `ProgramNumber`

### Events
- _No public events are defined in this plugin._

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required and are referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the below command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.
