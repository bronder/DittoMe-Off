# DittoMe-Off

A lightweight clipboard manager for Windows with multi-format syntax highlighting, themes, and global hotkey support.

## Features

- **Clipboard History** - Automatically captures and stores clipboard content
- **Format Detection** - Automatically detects and highlights JSON, XML, HTML, C#, JavaScript, Python, SQL, CSS, Markdown, Shell and more
- **Code Syntax Highlighting** - Beautiful syntax highlighting for code snippets
- **Global Hotkey** - Toggle visibility with a custom keyboard shortcut
- **Cursor Positioning** - Window appears at cursor position with off-screen detection
- **Search & Filter** - Find content quickly with real-time search and content type filtering
- **Pin Items** - Keep important items from being automatically deleted
- **Multiple Themes** - Choose from 16 beautiful themes including Dracula, Nord, Monokai, Tokyo Night, and more, organized by Dark and Light categories
- **System Tray** - Runs quietly in the system tray
- **Auto-start** - Option to launch with Windows

## Screenshots

*(Add screenshots here)*

## Installation

### Download
Download the latest release from the [Releases page](https://github.com/bronder/DittoMe-Off/releases).

### Build from Source
```bash
git clone https://github.com/bronder/DittoMe-Off.git
cd DittoMe-Off
dotnet build -c Release
```

The executable will be in `src/DittoMe-Off/bin/Release/net10.0-windows/`

## Usage

### Basic Controls
- **↑/↓** - Navigate through clipboard history
- **Enter** - Select and copy item
- **Ctrl+F** - Focus search box
- **Esc** - Clear search / minimize to tray

### Settings
Access settings via File > Settings or the ⚙️ button:
- **General Tab** - Configure history limit and auto-start
- **Appearance Tab** - Choose from 16 themes
- **Advanced Tab** - Set global hotkey, clear history

## Configuration

Settings are stored in `%APPDATA%/DittoMe-Off/`

## Logging

DittoMe-Off uses NLog for structured logging. Log files are stored at:
`%LOCALAPPDATA%\DittoMe-Off\Logs\`

Logs rotate daily with 7-day retention. Set `minlevel` in `NLog.config` to control verbosity.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| ↑/↓ | Navigate list |
| Enter | Copy selected item |
| Ctrl+F | Search |
| Esc | Clear search / Hide window |
| Ctrl+, | Open settings |

## Supported Formats

- Plain Text
- Images
- JSON
- XML
- HTML
- C#
- JavaScript
- Python
- SQL

## Requirements

- Windows 10 or later
- .NET 10.0 Runtime

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

**Version: v1.5.2**


