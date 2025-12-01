<p align="center">
  <img src="Nexus/Package/icon.png" alt="Nexus" width="128">
</p>

<h1 align="center">Valheim-Nexus</h1>

<p align="center">
  <a href="https://github.com/Slatyo/Valheim-Nexus/releases"><img src="https://img.shields.io/github/v/release/Slatyo/Valheim-Nexus?style=flat-square" alt="GitHub release"></a>
  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square" alt="License: MIT"></a>
</p>

<p align="center">
  Network optimization mod for Valheim multiplayer.<br>
  Configurable bandwidth limits, packet compression, and queue management to reduce lag and desync.
</p>

## Features

- **Bandwidth Management** - Increase from vanilla's ~50KB/s to 512KB/s or unlimited
- **Packet Compression** - GZip compression reduces network traffic between Nexus clients
- **Queue Management** - Larger buffers prevent packet drops during high-traffic
- **Update Rate Control** - Configurable ZDO sync frequency with auto-adjustment
- **Network Statistics** - Real-time overlay showing bandwidth, compression, latency
- **Vanilla Compatible** - Non-Nexus players can still connect

## Installation

### Thunderstore (Recommended)
Install via [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).

### Manual
1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)
3. Place `Nexus.dll` in `BepInEx/plugins/`

**Note:** Server + Client install recommended for full benefits.

## Configuration

### BepInEx Config (`BepInEx/config/com.nexus.valheim.cfg`)

| Setting | Default | Description |
|---------|---------|-------------|
| SendRateLimit | 512000 | bytes/sec (0 = unlimited) |
| ReceiveRateLimit | 512000 | bytes/sec (0 = unlimited) |
| EnableCompression | true | GZip packet compression |
| CompressionLevel | 6 | 1-9, higher = more compression |
| OutgoingQueueSize | 49152 | Queue buffer size (48 KB) |
| ShowNetworkStats | false | Toggle statistics overlay |

### JSON Config (`BepInEx/config/Nexus.json`)

Server-enforced settings for rate limits and compression policies.

## Console Commands

```
nexus status    - Show current network status
nexus stats     - Show detailed statistics  
nexus reset     - Reset all statistics
nexus overlay   - Toggle stats overlay
```

## Why Nexus?

Valheim's vanilla networking has hard-coded bandwidth limits (~50KB/s) that cause:
- Rubber-banding and teleporting players
- Desync between players
- Lag spikes in new areas
- Poor Ashlands multiplayer performance

Nexus addresses these by allowing configurable limits, compression, and smarter queue management.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Acknowledgments

- Inspired by [BetterNetworking](https://thunderstore.io/c/valheim/p/CW_Jesse/BetterNetworking_Valheim/) by CW_Jesse
- Built using [JotunnModStub](https://github.com/Valheim-Modding/JotunnModStub) template
- Powered by [Jötunn](https://valheim-modding.github.io/Jotunn/) - the Valheim Library

## License

[MIT](LICENSE) © Slatyo
