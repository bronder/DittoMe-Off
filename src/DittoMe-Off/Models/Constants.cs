namespace DittoMeOff.Models;

/// <summary>
/// Centralized constants for the application.
/// </summary>
public static class AppConstants
{
    // Default values
    public const string DefaultHotkey = "Ctrl+Shift+V";
    public const int DefaultMaxHistoryCount = 100;
    public const bool DefaultAutoStart = false;

    // Database
    public const string DatabaseFolderName = "DittoMe-Off";
    public const string DatabaseFileName = "clipboard.db";
    public const string ConfigFileName = "config.json";

    // Auto-start registry
    public const string AutoStartRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    public const string AutoStartAppName = "DittoMe-Off";

    // UI Constants
    public const int PreviewTextMaxLength = 500;
    public const int FormattedContentMaxLength = 1000;
    public const int DebounceDelayMs = 300;

    // Messages
    public static class Messages
    {
        public const string ClearHistoryConfirm = "Are you sure you want to clear all unpinned clipboard history? Pinned items will be kept.";
        public const string ClearHistoryTitle = "Clear History";

        public const string ClearOlderThanDayConfirm = "Are you sure you want to clear all clipboard history older than 1 day? Pinned items will be kept.";
        public const string ClearOlderThanDayTitle = "Clear Older Than 1 Day";

        public const string ClearOlderThanWeekConfirm = "Are you sure you want to clear all clipboard history older than 1 week? Pinned items will be kept.";
        public const string ClearOlderThanWeekTitle = "Clear Older Than 1 Week";

        public const string ClearOlderThanMonthConfirm = "Are you sure you want to clear all clipboard history older than 1 month? Pinned items will be kept.";
        public const string ClearOlderThanMonthTitle = "Clear Older Than 1 Month";

        public const string HotkeyConflictTitle = "Hotkey Conflict";
        public static string HotkeyConflictMessage(string hotkey) => 
            $"The hotkey '{hotkey}' is already in use by another application. Please try a different combination.";
        public static string HotkeyInvalidMessage(string hotkey) => 
            $"The hotkey '{hotkey}' is invalid.";
        public static string HotkeyFailedMessage(string hotkey) => 
            $"Failed to register hotkey '{hotkey}'.";
    }

    // Context Menu
    public static class ContextMenu
    {
        public const string Copy = "Copy";
        public const string Pin = "Pin";
        public const string Unpin = "Unpin";
        public const string Delete = "Delete";
    }

    // Hotkey hint texts
    public static class HotkeyHints
    {
        public const string Recording = "Press your hotkey combination...";
        public const string Idle = "Click here, then press your hotkey combination...";
        public static string RecordingFormat(string hotkey) => $"Recording: {hotkey}";
    }

    // Filter type names (matches ContentFormatType enum)
    public static class FilterTypes
    {
        public const string Text = "Text";
        public const string Image = "Image";
        public const string Json = "Json";
        public const string Xml = "Xml";
        public const string Yaml = "Yaml";
        public const string Markdown = "Markdown";
        public const string Html = "Html";
        public const string Css = "Css";
        public const string CSharp = "CSharp";
        public const string JavaScript = "JavaScript";
        public const string Sql = "Sql";
        public const string Python = "Python";
        public const string Shell = "Shell";
    }

    // SQL statements
    public static class SqlStatements
    {
        public const string InsertItem = @"
            INSERT INTO ClipboardItems (Content, ContentType, FormatType, Timestamp, IsPinned, AppSource, Size, PreviewText, ImageData)
            VALUES (@Content, @ContentType, @FormatType, @Timestamp, @IsPinned, @AppSource, @Size, @PreviewText, @ImageData)";

        public const string GetItems = @"
            SELECT Id, Content, ContentType, FormatType, Timestamp, IsPinned, AppSource, Size, PreviewText, ImageData
            FROM ClipboardItems
            ORDER BY IsPinned DESC, Timestamp DESC
            LIMIT @Limit";

        public const string DeleteItem = "DELETE FROM ClipboardItems WHERE Id = @Id";

        public const string TogglePin = "UPDATE ClipboardItems SET IsPinned = NOT IsPinned WHERE Id = @Id";

        public const string ClearHistoryKeepPinned = "DELETE FROM ClipboardItems WHERE IsPinned = 0";

        public const string ClearHistoryAll = "DELETE FROM ClipboardItems";

        public const string ClearItemsOlderThanKeepPinned = "DELETE FROM ClipboardItems WHERE IsPinned = 0 AND Timestamp < @CutoffTime";

        public const string ClearItemsOlderThanAll = "DELETE FROM ClipboardItems WHERE Timestamp < @CutoffTime";

        public const string GetItemCount = "SELECT COUNT(*) FROM ClipboardItems";

        public const string AlterTableAddFormatType = "ALTER TABLE ClipboardItems ADD COLUMN FormatType INTEGER DEFAULT 0";
    }

    // Clear history options
    public static class ClearHistoryOptions
    {
        public const string All = "all";
        public const string Day = "day";
        public const string Week = "week";
        public const string Month = "month";
    }

    // Image descriptions
    public static string ImageDescription(int width, int height) => $"Image {width}x{height}";

    public static string FilesDescription(int count) => 
        count == 1 ? null! : $"{count} files";

    public const string TruncationSuffix = "...";
    public const string TruncatedMessage = "\n\n[...truncated...]";
}
