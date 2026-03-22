# Release v1.5.0

## What's New

### Logging Infrastructure
- Added NLog logging framework with rolling file logs
- Implemented structured logging with contextual properties
- Added global exception handling with stack trace capture
- Log files stored in `%LOCALAPPDATA%\DittoMe-Off\Logs\`

### Services Updated
- ClipboardMonitorService: Added Start/Stop lifecycle logging
- DatabaseService: Added connection lifecycle logging  
- ConfigService: Added configuration load/save logging
- ThemeService: Added theme loading and fallback handling
- HotkeyService: Added registration/unregistration logging
- GlobalExceptionHandler: Catches unhandled UI thread, non-UI thread, and task exceptions

### Bug Fixes
- Fixed Debug.WriteLine calls replaced with proper NLog logging
- MessageBox on fatal errors now only shows when debugger attached

---

## Previous Release (v1.4.1)

_(Add your release notes here)_

---

## Previous Release (v1.4.0)

# Release v1.4.0

## What's New

### Architecture Improvements
- **Dependency Injection** - Implemented proper DI using Microsoft.Extensions.DependencyInjection
  - All services now implement interfaces for better abstraction
  - Services are registered in a central DI container in App.xaml.cs
  - Enables unit testing and makes the codebase more maintainable
  - Constructor injection used throughout the application

### New Dependencies
- Added `Microsoft.Extensions.DependencyInjection` v8.0.0

---

## Previous Release (v1.3.2)

# Release v1.3.2

## What's New

_(Add your release notes here)_

---

## Previous Release (v1.3.1)

# Release v1.3.1

## What's New

### Theme Text Color Fixes
- **Fixed ListView text color** - ListView items now properly use theme's TextBrush instead of defaulting to black
- **Fixed ComboBox text color** - ComboBox controls now properly use theme's TextBrush for dropdown items
- **Fixed TextBox text color** - Search box and Hotkey input fields now properly use theme's TextBrush
- **Organized theme selection** - Themes in Settings are now grouped by Dark and Light categories for easier browsing

---

## Previous Release (v1.3.0)

# Release v1.3.0

## What's New

### UI Styling Improvements
- **Rounded ComboBox controls** - Filter dropdown and Clear History dropdown now have rounded corners (CornerRadius="6")
- **Rounded TextBox controls** - Search box and Hotkey input now have rounded corners for consistent styling
- **Theme-aware controls** - All rounded controls use DynamicResource for proper theme support

### Window Positioning
- Window appears at cursor position when hotkey is pressed
- Automatic off-screen detection - position is adjusted if window would extend beyond screen edges

### Previous Release (v1.1.0)

### UI Improvements
- **Frameless window** with custom title bar and window controls (minimize, maximize, close)
- **Content type filter dropdown** - filter by format (CSS, C#, HTML, Image, JS, JSON, Markdown, Python, Shell, SQL, Text, XML, YAML)
- **Window border** for better visibility against light backgrounds
- Styled settings button matching the settings page design

---

## Previous Release (v1.0.0)

Initial release of DittoMe-Off - a lightweight clipboard manager for Windows.

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

Extract the zip and run `DittoMe-Off.exe`. Requires .NET 10.0 Runtime.

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

**Full Changelog**: https://github.com/bronder/DittoMe-Off/commits


---

**Full Changelog**: https://github.com/bronder/DittoMe-Off/commits


---

**Full Changelog**: https://github.com/bronder/DittoMe-Off/commits

---

**Full Changelog**: https://github.com/bronder/DittoMe-Off/commits
