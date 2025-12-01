# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
