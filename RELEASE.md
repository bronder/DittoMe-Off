# Release v1.1.0

## What's New

### Window Positioning
- Window now appears at cursor position when hotkey is pressed
- Automatic off-screen detection - position is adjusted if window would extend beyond screen edges

### UI Improvements
- **Frameless window** with custom title bar and window controls (minimize, maximize, close)
- **Content type filter dropdown** - filter by format (CSS, C#, HTML, Image, JS, JSON, Markdown, Python, Shell, SQL, Text, XML, YAML)
- **Window border** for better visibility against light backgrounds
- Styled settings button matching the settings page design

### Git Ignore
- Added *.zip, *.rar, *.7z to ignore list

---

## Previous Release (v1.0.0)

Initial release of DittoMeOff - a lightweight clipboard manager for Windows.

### Core Features
- **Clipboard History** - Automatically captures and stores clipboard content
- **Format Detection** - Automatically detects JSON, XML, HTML, C#, JavaScript, Python, SQL and more
- **Syntax Highlighting** - Beautiful code highlighting in preview pane
- **Search Highlighting** - Find matches highlighted in both list and preview panes
- **Global Hotkey** - Toggle visibility with custom keyboard shortcut
- **Pin Items** - Keep important clips from being auto-deleted

### Themes
16 beautiful themes including:
- Dark, Dracula, Monokai, Nord, Tokyo Night, Synthwave, Gruvbox
- Light themes: Light, Ayu Light, Catppuccin, Daylight, Everforest, GitHub, One Light, Solarized

### Settings
- Configurable history limit (1-10,000 items)
- Auto-start with Windows option
- Clear history (all, older than 1 day/week/month)

## Installation

Extract the zip and run `DittoMeOff.exe`. Requires .NET 10.0 Runtime.

## Requirements

- Windows 10 or later
- .NET 10.0 Runtime

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| ↑/↓ | Navigate list |
| Enter | Copy selected item |
| Ctrl+F | Search |
| Esc | Clear search / Hide |
| Ctrl+, | Settings |

---

**Full Changelog**: https://github.com/yourusername/dittoMeOff/commits
