# PDT.EssentialsPluginTemplate.EPI


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