# Nexus - Valheim Network Optimizer

A modern networking optimization mod for Valheim that addresses multiplayer performance issues, especially relevant after the Ashlands update.

## Features

### Bandwidth Management
- **Configurable send/receive rate limits** - Increase from vanilla's ~50KB/s to 512KB/s or higher
- **Unlimited bandwidth mode** - Remove all bandwidth caps for high-quality connections
- **Server-enforced limits** - Admins can set maximum allowed rates via JSON config

### Packet Compression
- **GZip compression** - Reduces network traffic between Nexus-enabled clients
- **Configurable compression level** - Balance between CPU usage and compression ratio
- **Smart compression** - Only compresses packets above a configurable threshold
- **Vanilla compatibility** - Falls back gracefully for non-Nexus clients

### Queue Management
- **Larger outgoing queues** - Prevent packet drops during high-traffic situations
- **Connection buffer** - Smooth out network spikes for more consistent gameplay
- **Per-connection tracking** - Monitor queue usage per player

### Update Rate Optimization
- **Configurable update rates** - Adjust ZDO synchronization frequency
- **Auto-adjustment** - Automatically reduce rates when connection quality is poor
- **Minimum rate floor** - Prevent rates from dropping too low

### Statistics & Debugging
- **Network statistics** - Track send/receive rates, compression savings, queue usage
- **Connection quality score** - At-a-glance view of network health
- **Debug logging** - Detailed logs for troubleshooting

## Configuration

### BepInEx Config (com.nexus.valheim.cfg)
Located in `BepInEx/config/`

```ini
[1. Bandwidth]
SendRateLimit = 512000      # bytes/sec (0 = unlimited)
ReceiveRateLimit = 512000   # bytes/sec (0 = unlimited)
UnlimitedBandwidth = false  # Remove all limits

[2. Compression]
EnableCompression = true
CompressionLevel = 6        # 1-9
CompressionThreshold = 128  # Minimum bytes before compressing

[3. Queue]
OutgoingQueueSize = 49152   # 48 KB
ConnectionBufferSize = 65536 # 64 KB

[4. Update Rate]
DefaultUpdateRate = 100     # percentage
MinUpdateRate = 50          # percentage
AutoAdjustUpdateRate = true

[5. Debug]
ShowNetworkStats = false
DebugMode = false
LogNetworkEvents = false
```

### JSON Config (Nexus.json)
Located in `BepInEx/config/` - Server-enforced settings

```json
{
  "SendRateLimit": 512000,
  "ReceiveRateLimit": 512000,
  "EnableCompression": true,
  "ForceCompression": false,
  "AllowClientOverride": true,
  "MaxClientSendRate": 1000000,
  "MaxClientReceiveRate": 1000000
}
```

## Recommended Settings

### Home/Local Network (< 5ms ping)
```ini
SendRateLimit = 0           # Unlimited
ReceiveRateLimit = 0        # Unlimited
CompressionLevel = 3        # Fast compression
```

### Good Internet (< 50ms ping)
```ini
SendRateLimit = 1000000     # 1 MB/s
ReceiveRateLimit = 1000000  # 1 MB/s
CompressionLevel = 6        # Balanced
```

### Slower Internet (> 100ms ping)
```ini
SendRateLimit = 256000      # 256 KB/s
ReceiveRateLimit = 256000   # 256 KB/s
CompressionLevel = 9        # Maximum compression
AutoAdjustUpdateRate = true
```

## Compatibility

- **Works with vanilla clients** - Non-Nexus players can still connect
- **Server + Client install recommended** - Full benefits require both
- **Jotunn dependency** - Requires Jotunn 2.26.1+

### Known Conflicts
- May conflict with other networking mods (BetterNetworking, Network by Smoothbrain)
- Use only one network optimization mod at a time

## Installation

1. Install BepInEx 5.4+
2. Install Jotunn 2.26.1+
3. Place `Nexus.dll` in `BepInEx/plugins/`
4. Configure settings as needed

## Troubleshooting

### Still experiencing lag?
1. Enable `ShowNetworkStats` to see current bandwidth usage
2. If utilization is high, try increasing rate limits
3. Enable `DebugMode` for detailed logs

### Players desyncing?
1. Ensure all players have Nexus installed
2. Check that server's JSON config allows client overrides
3. The player closest to action should have the best connection

### Ashlands still laggy?
1. Ashlands sends significantly more data than other biomes
2. Try increasing rate limits to 1MB/s or higher
3. Ensure compression is enabled

## Credits

- **Author:** Slatyo
- **Inspired by:** BetterNetworking by CW_Jesse
- **Built with:** Jotunn, the Valheim Library

## Links

- [GitHub](https://github.com/Slatyo/Valheim-Nexus)
- [Thunderstore](https://thunderstore.io/c/valheim/)
- [Bug Reports](https://github.com/Slatyo/Valheim-Nexus/issues)
