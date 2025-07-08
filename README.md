# Video-To-Ascii

A modern C# console application that converts videos into ASCII art with audio support.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

Video-To-Ascii transforms video files into ASCII art representations that can be viewed directly in the console. The application leverages the latest C# features to provide a responsive and efficient video-to-text conversion experience with synchronized audio playback.

Thanks to [Joel Ibaceta/video-to-ascii](https://github.com/joelibaceta/video-to-ascii) for the original project.

## Features

- Convert video files to real-time ASCII art in your console
- Multiple conversion strategies for different artistic styles
- Audio support for a complete viewing experience
- Support for various video formats
- Support for outputting the result to a file

## Requirements

- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [FFmpeg](https://ffmpeg.org/) for audio support
- Terminal/console with Unicode support
- Sufficient console size for the desired ASCII resolution

## Installation

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
- [FFmpeg](https://ffmpeg.org/) for video processing capabilities
- [NAudio](https://github.com/naudio/NAudio) for audio processing
- All contributors who have helped shape this project
