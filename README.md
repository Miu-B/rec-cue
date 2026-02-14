# Rec-Cue

![rec-cue icon](rec-cue/rec-cue.png)

A Dalamud plugin that monitors configurable folders for recording file activity and shows an in-game recording indicator when capture is likely active.

## Features

* **File Activity Monitoring**
  * Monitors up to 5 folders simultaneously for file write/modification activity using FileSystemWatcher
  * Recursive monitoring of subdirectories
  * Automatic recovery on watcher errors
  * Grace period for buffered writers (e.g. OBS) to prevent indicator flickering

* **Visual Recording Indicator**
  * Always-visible on-screen indicator
  * **Active** (Red pulsing dot): File activity detected within 5 seconds
  * **OnHold** (Grey dot): No activity for 5+ seconds
  * **Error** (Orange dot): A configured folder path doesn't exist
  * Adjustable indicator position and scale (0.5x - 2.0x)
  * Auto-restart monitoring when folder error is resolved

* **Simple Configuration**
  * Multi-folder support with per-folder validation
  * Built-in folder picker dialog
  * Real-time validation
  * Persistent configuration with automatic migration

## Installation

Rec-Cue is available in the official Dalamud plugin installer. Open the Plugin Installer in-game and search for **Rec-Cue**.

## How To Use

### Getting Started

1. Open the configuration window using `/reccue` command
2. Click "Browse" to select a folder to monitor (e.g., your recording software's output folder)
3. Add additional folders with "+ Add Folder" (up to 5)
4. The indicator will appear on screen:
   * **Red pulsing** = Recording in progress (file activity detected within 5 seconds)
   * **Grey** = No recording activity
   * **Orange** = A configured folder path doesn't exist

### Commands

* `/reccue` - Toggle the configuration window
* `/reccue show` - Show the indicator
* `/reccue hide` - Hide the indicator

### Adjusting Indicator

1. Open the configuration window with `/reccue`
2. While the configuration window is open, drag the indicator to your desired position
3. Use the "Indicator Scale" slider to adjust size
4. Position is saved automatically when you finish dragging

## Configuration

All settings are saved automatically:

* **Monitored Folders** - Up to 5 folder paths to monitor for file activity
* **Indicator Scale** - Scale multiplier for the indicator (0.5x to 2.0x)
* **Hide Indicator** - Toggle indicator visibility

## Development

### Building

```bash
dotnet build --configuration Release
```

### Running Tests

```bash
dotnet test
```

### Loading in Dalamud (Dev)

1. Launch the game and use `/xlsettings` to open Dalamud settings
2. Go to `Experimental` and add the full path to `rec-cue.dll` to Dev Plugin Locations
3. Use `/xlplugins` to open the Plugin Installer
4. Go to `Dev Tools > Installed Dev Plugins` and enable rec-cue
5. Use `/reccue` to open the configuration window

## License

AGPL-3.0-or-later

## Credits

Based on [SamplePlugin](https://github.com/goatcorp/SamplePlugin) template by goatcorp
