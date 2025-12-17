# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0](https://github.com/Slatyo/Valheim-Nexus/compare/v1.0.0...v1.1.0) (2025-12-17)


### Features

* initial release of Nexus network optimization mod ([ecc4157](https://github.com/Slatyo/Valheim-Nexus/commit/ecc4157e24862726a49efbad6e6def136f01883b))


### Code Refactoring

* migrate commands to Munin framework ([baa0bde](https://github.com/Slatyo/Valheim-Nexus/commit/baa0bdef83b1c160fa7e20abcdec39aaaeacdd61))
* migrate debug overlay to Veneer UI framework ([1eb6bc0](https://github.com/Slatyo/Valheim-Nexus/commit/1eb6bc0154100353e3c1c198763f013cee656864))
* unify plugin GUID to com.slatyo.nexus ([95aa556](https://github.com/Slatyo/Valheim-Nexus/commit/95aa5563b1a52d5daa15f83e5a43a480abfb4986))

## [1.0.0] - 2025-12-01

### Added
- Configurable bandwidth limits (send/receive rate limiting)
- GZip packet compression with configurable level
- Queue management with larger outgoing buffers
- Connection buffer for handling packet bursts
- Update rate configuration with auto-adjustment
- Network statistics tracking and overlay
- Console commands for status and diagnostics
- Server-enforced JSON configuration
- Vanilla client compatibility (graceful fallback)
