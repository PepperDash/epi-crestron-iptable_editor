# PDT.EssentialsPluginTemplate.EPI


## Config Example 
```json

            {
                "key": "IPTableEditor",
                "uid": 1,
                "name": "IPTableEditor",
                "type": "IPTableEditor",
                "group": "utilities",
                "properties": {
                    "RunAtStartup": true,
                    "IPTableChanges": [
                        {
                            "IpId": "0B",
                            "IpAddress": "192.168.1.150",
                            "ProgramNumber": 4
                        },
                        {
                            "IpId": "0C",
                            "IpAddress": "192.168.1.151",
                            "ProgramNumber": 4
                        }
                        ,
                        {
                            "IpId": "1C",
                            "IpAddress": "192.168.1.160",
                            "ProgramNumber": 5
                        }
                    ]
                }
            },
            // Note when "RunAtStartup": true bridge is not required. 
            {
                "key": "IPTableEditor-Bridge",
                "uid": 3,
                "name": "IP Table Editor-Bridge",
                "group": "api",
                "type": "eiscApi",
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
                            "deviceKey": "IPTableEditor",
                            "joinStart": 1
                        }
                    ]
                }
            }
```


``` C#
	public class IPTableEditorConfigObject
	{
		public List<IPTableObject> IPTableChanges { get; set; } 
	}
	
	public class IPTableObject
	{
		[JsonProperty("ipId")]
		public string IpId { get; set; }

		[JsonProperty("devId")]
		public string DevID { get; set; }

		[JsonProperty("ipPort")]
		public int IpPort { get; set; }

		[JsonProperty("ipAddress")]
		public string IpAddress { get; set; }

		[JsonProperty("programNumber")]
		public int ProgramNumber { get; set; }
	}
``` 

``` C#
	public class IPTableEditorTemplateBridgeJoinMap : JoinMapBase
	{
		public uint CheckTable { get; set; }
		public IPTableEditorTemplateBridgeJoinMap() 
		{
			CheckTable = 1;
		}

		public override void OffsetJoinNumbers(uint joinStart)
		{
			var Offset = joinStart - 1;
			CheckTable += Offset;
		}
,,,

