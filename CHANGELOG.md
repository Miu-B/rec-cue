# Changelog

All notable changes to this project will be documented in this file.

## [0.0.3.0] - 2026-02-14

### Added
- Multi-folder monitoring: configure up to 5 folders simultaneously (e.g. Steam Game Recording + OBS output)
- Config migration from single-folder (v0) to multi-folder (v1) format — existing configs upgrade automatically
- `FileWatcherManager` class to orchestrate multiple `FileWatcherService` instances
- Per-folder validation in the config window with individual error messages
- Tests for `FileWatcherManager` and updated `RecCueConfiguration` tests

### Fixed
- OBS indicator flashing caused by buffered writes: added poll grace period (`MaxInactivityTicks = 5`) so the poll timer survives gaps between OBS buffer flushes, and increased the inactivity timeout from 3 to 5 seconds so the indicator stays lit between flushes

### Changed
- Config window redesigned with scrollable folder list, per-row Browse/Remove buttons, and Add Folder button
- Orange warning indicator now shows when ANY configured non-empty folder path is invalid

## [0.0.2.0] - 2026-02-14

### Changed
- Replaced polling-based file monitoring with `FileSystemWatcher` for more efficient and responsive detection
- Updated CI release workflow to pass version from git tag to build

### Added
- xUnit test project with tests for `RecordingDetectionLogic`, `FileWatcherService`, and `RecCueConfiguration`
- CHANGELOG.md for tracking release history

### Fixed
- Assembly version now correctly set in `.csproj` so the in-game plugin list shows the actual release version
- README updated to reflect official Dalamud plugin repository availability
- Fixed documentation references from 5-second to 3-second inactivity timeout
- Set `LoadPriority` to `0` in repo manifests

## [0.0.1.1] - 2025

### Fixed
- Improved thread safety and centralized folder validation

## [0.0.1.0] - 2025

### Added
- Indicator visibility control with config checkbox
- `/reccue show` and `/reccue hide` commands

## [0.0.0.4] - 2025

### Changed
- Better icon management
- Changed download links from rec-cue.zip to latest.zip (standard Dalamud convention)

## [0.0.0.3] - 2025

### Fixed
- Fixed IconUrl paths from main to master branch

## [0.0.0.2] - 2025

### Fixed
- Fixed IconUrl path from main to master
- Fixed branch name from main to master in workflow and docs

## [0.0.0.1] - 2025

### Added
- Initial release
- File activity monitoring with polling-based detection
- Visual recording indicator (red pulsing/grey/orange states)
- Configurable folder path with built-in folder picker
- Adjustable indicator position and scale
- Hide indicator during cutscenes, character selection, loading screens, and GPose
- Plugin icon
