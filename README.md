# <img src="https://raw.githubusercontent.com/Der-Floh/Video-To-Ascii/refs/heads/main/VideoToAscii/Resources/icon.png" height="30"> Video-To-Ascii

[![GitHub release)](https://img.shields.io/github/v/release/Der-Floh/Video-To-Ascii)](https://github.com/Der-Floh/Video-To-Ascii/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/Der-Floh/Video-To-Ascii/latest/total)](https://github.com/Der-Floh/Video-To-Ascii/releases/latest)
[![GitHub issues](https://img.shields.io/github/issues/Der-Floh/Video-To-Ascii)](https://github.com/Der-Floh/Video-To-Ascii/issues)

A modern C# console application that converts videos into ASCII art with audio support.

## Overview

Video-To-Ascii transforms video files into ASCII art live in the console. The application provides a responsive and efficient video-to-text conversion experience with synchronized audio playback.
Additionally exporting to different output formats like `.json`, `.ps1` or `.sh` is supported.

Thanks to [Joel Ibaceta/video-to-ascii](https://github.com/joelibaceta/video-to-ascii) for the original project.

## Features

- Convert video files to real-time ASCII art in your console
- Multiple conversion strategies for different artistic styles
- Audio support for a complete viewing experience
- Support for various video formats
- Support for outputting the result to a file

## Requirements

- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [FFmpeg](https://ffmpeg.org/) for audio support (optional)
- Terminal/console with Unicode support
- Sufficient console size for the desired ASCII resolution

## Installation

[![Chocolatey](https://img.shields.io/badge/chocolatey-under_moderation-80B5E3)](https://community.chocolatey.org/packages/videotoascii)
[![Scoop](https://img.shields.io/badge/scoop-available-4CA146)](https://github.com/Der-Floh/scoop-bucket?tab=readme-ov-file#available-apps)
[![WinGet](https://img.shields.io/badge/winget-available-0063B1)](https://winget.run/search?query=videotoascii)

### <img src="https://upload.wikimedia.org/wikipedia/commons/4/48/Chocolatey_icon.svg" height="16"> Chocolatey

Currently under moderation. Therefore only 1.1.2 is available and version needs to be specified.

Install Chocolatey and open a terminal:
```bash
choco install videotoascii --version 1.1.2
```

### <img src="https://avatars.githubusercontent.com/u/16618068" height="16"> Scoop

Install Scoop and add My Scoop bucket and the versions bucket:
```bash
scoop bucket add der_floh https://github.com/der-floh/scoop-bucket
scoop bucket add versions
```

Then install the tool:
```
scoop install videotoascii
```

### <img src="https://raw.githubusercontent.com/microsoft/winget-cli/master/.github/images/WindowsPackageManager_Assets/ICO/PNG/_64.png" height="16"> WinGet

Install WinGet and run a terminal with administrator privileges (required for dotnet):
```bash
winget install videotoascii
```

### Releases

Download and install the latest [Release](https://github.com/Der-Floh/Video-To-Ascii/releases/latest) or view all [Releases](https://github.com/Der-Floh/Video-To-Ascii/releases).

## Usage

### Options

```
Options:
  -f, --file <path>        Path to the video file (required)
  -s, --strategy <name>    Conversion strategy (default: filled-ascii)
                           Available: filled-ascii, ascii-color, just-ascii
  -a, --audio <bool>       Enable/disable audio (default: false)
  -o, --output <path>      Output file path (default: none)
  --help                   Display help information
```

#### Supported Output types
These are the current supported output file types:
- `.txt`
- `.json`
- `.bat`
- `.ps1`
- `.sh`

### Examples

```bash
# Basic playback with default settings
videotoascii -f video.mp4

# Just-ascii conversion with audio
videotoascii -f video.mp4 -s just-ascii -a

# Output to file
videotoascii -f video.mp4 -o output.ps1
```

## Acknowledgments

- [Joel Ibaceta/video-to-ascii](https://github.com/joelibaceta/video-to-ascii) for the original project

<br>

[!["Buy me a Floppy Disk"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/der_floh)
