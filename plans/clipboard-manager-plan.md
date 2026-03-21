# Clipboard Manager Application - DittoMe-Off

## Project Overview
A Windows clipboard manager application that keeps a history of clipboard contents, is user-configurable, supports global hotkeys, and provides a UI to browse and manage clipboard history.

## Core Features

### 1. Clipboard Monitoring Service
- Background service that monitors Windows clipboard for changes
- Capture text, images, and file paths
- Store clipboard history with timestamps
- Persist history to local storage/database
- Configurable history limit (number of items to keep)

### 2. Global Hotkey System
- Register system-wide hotkey to show/hide clipboard manager
- Default: Ctrl+Shift+V (configurable)
- Show popup window at cursor position or screen center

### 3. Clipboard History UI
- Main window displaying list of clipboard items
- Search/filter functionality
- Preview pane for selected item
- Copy item back to clipboard on click/double-click
- Delete individual items
- Clear all history option

### 4. Configuration System
- Maximum history count
- Hotkey customization
- Auto-start with Windows option
- Storage location
- Exclude applications from monitoring
- Maximum item size to capture

## Technical Architecture

### Technology Stack
- **Framework**: Electron with TypeScript
- **UI**: React with modern CSS
- **Storage**: SQLite via better-sqlite3
- **Build**: electron-builder

### Key Modules
```
src/
├── main/                    # Electron main process
│   ├── clipboard-monitor.ts  # Clipboard watching service
│   ├── hotkey-manager.ts    # Global hotkey registration
│   ├── storage.ts          # SQLite database operations
│   ├── window-manager.ts   # Window creation and management
│   └── ipc-handlers.ts     # IPC communication handlers
├── renderer/               # React frontend
│   ├── App.tsx            # Main application component
│   ├── components/
│   │   ├── ClipboardList.tsx
│   │   ├── ClipboardItem.tsx
│   │   ├── SearchBar.tsx
│   │   ├── Settings.tsx
│   │   └── Preview.tsx
│   └── hooks/
│       └── useClipboard.ts
└── shared/
    └── types.ts           # Shared TypeScript interfaces
```

## UI/UX Design

### Main Window
- Compact, floating window (400-500px width)
- Search bar at top
- Scrollable list of clipboard items
- Each item shows: preview text, timestamp, type indicator
- Context menu for item actions (copy, delete, pin)
- Settings gear icon

### Settings Panel
- Tabbed interface or slide-out panel
- Categories: General, Hotkeys, Storage, Exclusions

### Visual Style
- Modern, clean interface
- System tray integration
- Semi-transparent/frameless window option
- Dark/light theme support

## Data Model

### ClipboardItem
```typescript
interface ClipboardItem {
  id: string;
  content: string;
  contentType: 'text' | 'image' | 'file' | 'html';
  timestamp: number;
  pinned: boolean;
  appSource?: string;
  size: number;
}
```

### Config
```typescript
interface Config {
  maxHistoryCount: number;
  hotkey: string;
  autoStart: boolean;
  theme: 'light' | 'dark' | 'system';
  maxItemSize: number;
  excludedApps: string[];
}
```

## Implementation Phases

### Phase 1: Core Infrastructure
- Set up Electron project with TypeScript and React
- Implement clipboard monitoring service
- Set up SQLite storage
- Basic window management

### Phase 2: UI Development
- Build clipboard list component
- Implement search/filter
- Create preview pane
- Add settings panel

### Phase 3: Advanced Features
- Global hotkey registration
- System tray integration
- Configuration persistence
- Context menu actions

### Phase 4: Polish & Distribution
- Theme support
- Performance optimization
- Build and packaging
- Testing and bug fixes
