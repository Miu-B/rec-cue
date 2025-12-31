# Rec-Cue

![rec-cue icon](rec-cue/rec-cue.png)

A Dalamud plugin that monitors a configurable folder for recording file activity and shows an in-game recording indicator when capture is likely active.

## Features

* **File Activity Monitoring**
  * Monitors a folder for file write/modification activity
  * Recursive monitoring of subdirectories
  * Adjustable inactivity timeout (default: 5 seconds)

* **Visual Recording Indicator**
  * Always-visible on-screen indicator
  * **Active** (Red pulsing dot): File activity detected within 5 seconds
  * **OnHold** (Grey dot): No activity for 5+ seconds
  * **Error** (Orange dot): No folder configured or folder doesn't exist
  * Adjustable indicator position and scale (0.5x - 2.0x)
  * Auto-restart monitoring when folder error is resolved

* **Simple Configuration**
  * Built-in folder picker dialog
  * Real-time validation
  * Persistent configuration

## How To Use

### Getting Started

1. Build the plugin and load it in Dalamud
2. Open the configuration window using `/reccue` command
3. Click "Browse..." to select a folder to monitor (e.g., your recording software's output folder)
4. The indicator will appear on screen:
   * **Red pulsing** = Recording in progress (file activity detected within 5 seconds)
   * **Grey** = No recording activity
   * **Orange** = No folder configured or non existing

### Adjusting Indicator

1. Open the configuration window with `/reccue`
2. While the configuration window is open, drag the indicator to your desired position
3. Use the "Indicator Scale" slider to adjust size
4. Position is saved automatically when you finish dragging

### Loading in Dalamud

1. Launch the game and use `/xlsettings` to open Dalamud settings
2. Go to `Experimental` and add the full path to `rec-cue.dll` to Dev Plugin Locations
3. Use `/xlplugins` to open the Plugin Installer
4. Go to `Dev Tools > Installed Dev Plugins` and enable rec-cue
5. Use `/reccue` to open the configuration window

## Configuration

All settings are saved automatically:

* **Monitored Folder** - Path to the folder being monitored for file activity
* **Indicator Scale** - Scale multiplier for the indicator (0.5x to 2.0x)

## License

AGPL-3.0-or-later

## Credits

Based on [SamplePlugin](https://github.com/goatcorp/SamplePlugin) template by goatcorp
