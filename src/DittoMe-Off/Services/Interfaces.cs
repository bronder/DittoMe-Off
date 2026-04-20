using DittoMeOff.Models;

namespace DittoMeOff.Services;

public interface IConfigService
{
    AppConfig Config { get; }
    void Save();
    void UpdateConfig(Action<AppConfig> updateAction);
}

public interface IDatabaseService : IDisposable
{
    void Initialize();
    long InsertItem(ClipboardItem item);
    List<ClipboardItem> GetItems(int limit = 100);
    void DeleteItem(long id);
    void TogglePin(long id);
    void ClearHistory(bool keepPinned = true);
    void ClearItemsOlderThan(int days, bool keepPinned = true);
    int GetItemCount();
    void DeleteOldestExcessItems(int keepCount, bool keepPinned = true);
}

public interface IClipboardMonitorService : IDisposable
{
    event EventHandler<ClipboardItem>? ClipboardChanged;
    void Start();
    void Stop();
}

public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    void Initialize(System.Windows.Window window);
    HotkeyRegistrationResult RegisterHotkey(string hotkeyString);
    void UnregisterHotkey();
}

public interface IThemeService
{
    void ApplyTheme(AppTheme theme);
    void LoadSavedTheme();
}

public interface IWindowPositionService
{
    void PositionWindowNearWindow(System.Windows.Window window, IConfigService configService, IntPtr targetWindowHandle);
    void PositionWindowAtTopCenter(System.Windows.Window window, IConfigService configService);
}
