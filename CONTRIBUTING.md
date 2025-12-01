# Contributing to Valheim-Nexus

Thanks for your interest in contributing!

## Getting Started

1. Fork the repository
2. Clone your fork
3. Copy `Environment.props.example` to `Environment.props` and set your paths
4. Open `Nexus.sln` in Visual Studio or Rider
5. Build in Debug mode - the dll auto-deploys to your configured BepInEx plugins folder

## Development Setup

### Requirements
- Visual Studio 2022 or JetBrains Rider
- .NET Framework 4.8 SDK
- Valheim installed
- BepInEx and Jotunn installed in your Valheim instance

### Environment.props
Create `Environment.props` in the project root (it's gitignored):
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <VALHEIM_INSTALL>D:\Steam\steamapps\common\Valheim</VALHEIM_INSTALL>
    <BEPINEX_PATH>$(VALHEIM_INSTALL)\BepInEx</BEPINEX_PATH>
    <MOD_DEPLOYPATH>$(BEPINEX_PATH)\plugins</MOD_DEPLOYPATH>
  </PropertyGroup>
</Project>
```

## Making Changes

1. Create a branch: `git checkout -b feature/your-feature`
2. Make your changes
3. Test in-game (ideally with multiple clients)
4. Commit with clear messages
5. Push and open a Pull Request

## Testing Network Changes

- Test with both Nexus and vanilla clients
- Test with different ping/latency conditions
- Monitor bandwidth usage with the overlay enabled
- Check compression ratios are reasonable

## Code Style

- Follow existing code patterns
- Keep methods focused and small
- Use meaningful names
- Add XML documentation to public APIs

## Reporting Issues

- Check existing issues first
- Include Valheim version, mod version, and BepInEx log
- Describe steps to reproduce
- Include network statistics if relevant

## Questions?

Open an issue or discussion on GitHub.
