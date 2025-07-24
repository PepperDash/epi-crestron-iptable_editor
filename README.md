![PepperDash Logo](/images/essentials-plugin-blue.png)

# IP Table Editor Plugin

## Overview
The **IP Table Editor Plugin** is a PepperDash Essentials Plugin (EPI) that enables dynamic, runtime editing and management of Crestron IP Table entries for devices such as touchpanels, XPanels, and network clients. This plugin is designed for use in environments where device IP addresses, IP-IDs, or related network parameters may need to be updated without reloading the entire program or recompiling SIMPL Windows.

***Point of Clarification*** The SIMPL Windows application must previously include an IP ID entry for every IP ID revision required and the device IP ID must have the `remap` option enabled. This EPI tool dynamically enables IP ID entries and updates IP addresses. It does not create IP ID entires or move an IP ID from one ID to another. 

![Screenshot](/images/IP-ID-Remap.png)

## Features
- **Runtime IP Table Editing:** Modify IP address, IP-ID, port, and program assignment for supported devices.
- **SIMPL Windows Bridge Integration:** Exposes join map for control and feedback via Essentials Device Bridge (EISC).
- **Persistent and Selectable Entries:** Supports both persistent (always active) and selectable (user-swappable) IP table entries.
- **Startup Automation:** Optionally applies changes automatically at startup or via bridge commands.
- **Diagnostics:** Feedback joins for table status and change operations.

## Use Cases
This EPI is ideal for:
- Commissioning and service scenarios where device network assignments change frequently.
- Commissioning standard control code solutions with configurable design variations.
- Environments with hot-swappable or backup devices (e.g., spare touchpanels).
- Systems requiring remote or API-driven IP table management.
- Reducing downtime by avoiding full program reloads for simple network changes.

## SIMPL EISC Bridge Map
Below is a sample join map for the SIMPL EISC Bridge, showing the digital and analog joins exposed by the plugin. Adjust `joinStart` as needed in your Essentials Device Bridge configuration.

#### Digitals
| dig-o (Input/Triggers)     | I/O   | dig-i (Feedback)     |
|----------------------------|-------|----------------------|
| CheckTable                 | 1-10  | UpdateNeededFb       |
|                            | 11    |                      |
|                            | 12    |                      |
|                            | 13    |                      |
|                            | 14    |                      |

#### Analogs
| an_o (Input/Triggers) | I/O  | an_i (Feedback)            |
|-----------------------|------|----------------------------|
| Select Item           | 1    | Item Selected              |
|                       | 2    |                            |
|                       | 3    |                            |
|                       | 4    |                            |
|                       | 5    |                            |

#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback)  |
|---------------------------|-----|----------------------|
|                           | 1   |                      |
|                           | 2   |                      |
|                           | 3   |                      |
|                           | 4   |                      |
|                           | 5   |                      |

## License

Provided under MIT license

# PepperDash Essentials Utilities Route Cycle Plugin (c) 2023

This repo contains a plugin for use with [PepperDash Essentials](https://github.com/PepperDash/Essentials). 

## IP Table Editor Plugin Configuration
```json
{
    "key": "iptable-editor1-plugin",
    "uid": 1,
    "name": "IP Table Editor",
    "type": "iptableeditor",
    "group": "utilities",
    "properties": {
        "runAtStartup": false,
        "ipTableChanges": [
            {
                "name": "TP01 - Remappable",
                "ipId": "11",
                "ipAddress": "127.0.0.1",
                "devId": "11",
                "programNumber": 10
            },
            {
                "name": "TP01 Xpanel - Remappable",
                "ipId": "21",
                "ipAddress": "127.0.0.1",
                "devId": "21",
                "programNumber": 10
            },
            {
                "name": "TCP Client 1",
                "ipId": "81",
                "ipAddress": "192.168.1.151",
                "ipPort": 23,
                "programNumber": 10
            },
            {
                "name": "UDP Client 1",
                "ipId": "91",
                "ipAddress": "192.168.1.152",  
                "ipPort": 5000,                          
                "programNumber": 10
            }
        ]
    }
}
```
## Essentials Device Bridge
Note when "RunAtStartup": true bridge is not required. 
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
                "address": "127.0.0.2",
                "port": 0
            },
            "ipid": "A0",
            "method": "ipidTcp"
        },
        "devices": [
            {
                "deviceKey": "iptable-editor1-plugin",
                "joinStart": 1
            }
        ]
    }
}
```

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

## Github Actions

This repo contains two Github Action workflows that will build this project automatically. Modify the SOLUTION_PATH and SOLUTION_FILE environment variables as needed. Any branches named `feature/*`, `release/*`, `hotfix/*` or `development` will automatically be built with the action and create a release in the repository with a version number based on the latest release on the master branch. If there are no releases yet, the version number will be 0.0.1. The version number will be modified based on what branch triggered the build:

- `feature` branch builds will be tagged with an `alpha` descriptor, with the Action run appended: `0.0.1-alpha-1`
- `development` branch builds will be tagged with a `beta` descriptor, with the Action run appended: `0.0.1-beta-2`
- `release` branches will be tagged with an `rc` descriptor, with the Action run appended: `0.0.1-rc-3`
- `hotfix` branch builds will be tagged with a `hotfix` descriptor, with the Action run appended: `0.0.1-hotfix-4`

Builds on the `Main` branch will ONLY be triggered by manually creating a release using the web interface in the repository. They will be versioned with the tag that is created when the release is created. The tags MUST take the form `major.minor.revision` to be compatible with the build process. A tag like `v0.1.0-alpha` is NOT compatible and may result in the build process failing.

If you have any questions about the action, contact [Andrew Welker](mailto:awelker@pepperdash.com) or [Neil Dorin](mailto:ndorin@pepperdash.com).

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.