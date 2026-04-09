# Release v1.7.4

## What's New

### Smart Window Positioning
- **Window appears near active window** - When you press the global hotkey, the clipboard manager now appears next to the window you were just using
- **Smart placement** - Tries positions in order: right → below → left → above the active window
- **DPI-aware** - Properly scales coordinates for high-DPI displays
- **On-screen validation** - Ensures window stays visible within screen bounds

---

# Release v1.7.3

## Bug Fixes

- **Fixed window position drift** - Window no longer creeps toward the top-left corner on each show/hide cycle. The root cause was a DPI mismatch: `SetWindowPos` (Win32) expects physical pixels but was receiving WPF device-independent pixels (DIPs), causing progressive position corruption on displays with DPI scaling > 100%.

---

# Release v1.7.1

## What's New

_(Add your release notes here)_

---

# Release v1.6

## What's New

### Auto-Paste Feature
- **One-key copy and paste** - Press Enter to copy an item AND automatically paste it into the previously active application
- When you press your global hotkey, the app now captures which window was active
- After selecting an item with Enter, the content is pasted directly into your target application
- Uses Windows API (GetForegroundWindow, SetForegroundWindow, keybd_event) for reliable pasting

---

## Previous Release (v1.5.2)

### UI Improvements
- **Removed splitter max width restriction** - The list/preview splitter can now be dragged to any width
- **Unbolded list font** - List items now use normal font weight instead of bold
- **Format badge repositioned** - Format type badge now appears on the same line as preview text

### Preview Lines Configuration
- **Configurable preview lines** - New setting to control how many lines of preview text to show (default: 1)
- **Preset buttons** - Quick selection buttons for 1-5 lines
- **Freeform input** - Type any number for custom line count
- **Persisted setting** - Preview lines count is saved and restored on app restart

---

## Previous Release (v1.5.1)

_(Add your release notes here)_

---

## Previous Release (v1.5.0)

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


---

**Full Changelog**: https://github.com/bronder/DittoMe-Off/commits
