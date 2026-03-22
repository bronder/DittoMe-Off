using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DittoMeOff.Models;
using DittoMeOff.Services;

namespace DittoMeOff.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IConfigService _configService;
    private readonly IClipboardMonitorService _clipboardMonitor;
    private readonly IThemeService _themeService;
    private readonly IHotkeyService _hotkeyService;

    [ObservableProperty]
    private ObservableCollection<ClipboardItem> _clipboardItems = new();

    [ObservableProperty]
    private ClipboardItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedTypeFilter = string.Empty;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private string _currentHotkey = AppConstants.DefaultHotkey;

    [ObservableProperty]
    private int _maxHistoryCount = AppConstants.DefaultMaxHistoryCount;

    [ObservableProperty]
    private bool _autoStart;

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.Light;

    [ObservableProperty]
    private Dictionary<AppTheme, string> _themeOptions;

    private List<ClipboardItem> _allItemsCache = new();
    private readonly DispatcherTimer _filterDebounceTimer;

    public MainViewModel(
        IDatabaseService databaseService, 
        IConfigService configService, 
        IClipboardMonitorService clipboardMonitor, 
        IThemeService themeService, 
        IHotkeyService hotkeyService)
    {
        _databaseService = databaseService;
        _configService = configService;
        _clipboardMonitor = clipboardMonitor;
        _themeService = themeService;
        _hotkeyService = hotkeyService;

        _themeOptions = new Dictionary<AppTheme, string>
        {
            { AppTheme.AyuLight, "Ayu Light" },
            { AppTheme.CatppuccinLatte, "Catppuccin Latte" },
            { AppTheme.Dark, "Dark" },
            { AppTheme.Daylight, "Daylight" },
            { AppTheme.Dracula, "Dracula" },
            { AppTheme.EverforestLight, "Everforest Light" },
            { AppTheme.GitHubLight, "GitHub Light" },
            { AppTheme.Gruvbox, "Gruvbox" },
            { AppTheme.Light, "Light" },
            { AppTheme.Monokai, "Monokai" },
            { AppTheme.NightOwl, "Night Owl" },
            { AppTheme.Nord, "Nord" },
            { AppTheme.OneLight, "One Light" },
            { AppTheme.SolarizedLight, "Solarized Light" },
            { AppTheme.Synthwave, "Synthwave" },
            { AppTheme.TokyoNight, "Tokyo Night" }
        };

        _filterDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(AppConstants.DebounceDelayMs)
        };
        _filterDebounceTimer.Tick += OnFilterDebounceTimerTick;

        LoadSettings();
        LoadItems();
        
        _clipboardMonitor.ClipboardChanged += OnClipboardChanged;
    }

    private void LoadSettings()
    {
        var config = _configService.Config;
        CurrentHotkey = config.Hotkey;
        MaxHistoryCount = config.MaxHistoryCount;
        AutoStart = config.AutoStart;
        SelectedTheme = config.Theme;
    }

    private void LoadItems()
    {
        var items = _databaseService.GetItems(_configService.Config.MaxHistoryCount);
        _allItemsCache = items.ToList();
        ClipboardItems.Clear();
        foreach (var item in items)
        {
            ClipboardItems.Add(item);
        }
        ItemCount = ClipboardItems.Count;
    }

    private void OnClipboardChanged(object? sender, ClipboardItem item)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            // Update the cache as well
            _allItemsCache.Insert(0, item);
            
            // Remove duplicates from cache
            var duplicatesToRemove = _allItemsCache.Skip(1)
                .Where(i => i.Content == item.Content && i.ContentType == item.ContentType)
                .ToList();
            foreach (var dup in duplicatesToRemove)
            {
                _allItemsCache.Remove(dup);
            }

            // Enforce cache limit
            while (_allItemsCache.Count > MaxHistoryCount)
            {
                var lastNonPinned = _allItemsCache.LastOrDefault(i => !i.IsPinned);
                if (lastNonPinned != null)
                {
                    _allItemsCache.Remove(lastNonPinned);
                }
                else
                {
                    break;
                }
            }

            ClipboardItems.Insert(0, item);
            
            // Remove duplicates from visible list
            var toRemove = ClipboardItems.Skip(1)
                .Where(i => i.Content == item.Content && i.ContentType == item.ContentType)
                .ToList();
            foreach (var removeItem in toRemove)
            {
                ClipboardItems.Remove(removeItem);
            }

            // Enforce limit
            while (ClipboardItems.Count > MaxHistoryCount)
            {
                var lastNonPinned = ClipboardItems.LastOrDefault(i => !i.IsPinned);
                if (lastNonPinned != null)
                {
                    ClipboardItems.Remove(lastNonPinned);
                }
                else
                {
                    break;
                }
            }

            ItemCount = ClipboardItems.Count;
        });
    }

    [RelayCommand]
    private void CopyItem(ClipboardItem? item)
    {
        if (item == null) return;

        try
        {
            _clipboardMonitor.Stop();
            
            switch (item.ContentType)
            {
                case ContentType.Text:
                case ContentType.Html:
                    Clipboard.SetText(item.Content);
                    break;
                case ContentType.Image:
                    if (item.ImageData != null)
                    {
                        using var stream = new System.IO.MemoryStream(item.ImageData);
                        var image = new System.Windows.Media.Imaging.BitmapImage();
                        image.BeginInit();
                        image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        Clipboard.SetImage(image);
                    }
                    break;
                case ContentType.File:
                    var files = new System.Collections.Specialized.StringCollection();
                    files.AddRange(item.Content.Split('\n'));
                    Clipboard.SetFileDropList(files);
                    break;
            }
        }
        finally
        {
            // Restart monitoring after a short delay
            Task.Delay(500).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() => _clipboardMonitor.Start());
            });
        }
    }

    [RelayCommand]
    private void DeleteItem(ClipboardItem? item)
    {
        if (item == null) return;

        bool wasSelected = item == SelectedItem;
        int index = ClipboardItems.IndexOf(item);
        
        // Clear image cache to prevent memory leak
        item.ClearImageCache();
        
        _databaseService.DeleteItem(item.Id);
        _allItemsCache.Remove(item);
        ClipboardItems.Remove(item);
        ItemCount = ClipboardItems.Count;

        // If the deleted item was selected, select the next item
        if (wasSelected && ClipboardItems.Count > 0)
        {
            int newIndex = index >= ClipboardItems.Count ? ClipboardItems.Count - 1 : index;
            SelectedItem = ClipboardItems[newIndex];
        }
    }

    [RelayCommand]
    private void TogglePin(ClipboardItem? item)
    {
        if (item == null) return;
        
        _databaseService.TogglePin(item.Id);
        item.IsPinned = !item.IsPinned;
        
        // Re-sort items
        var sorted = ClipboardItems.OrderByDescending(i => i.IsPinned)
            .ThenByDescending(i => i.Timestamp)
            .ToList();
        
        ClipboardItems.Clear();
        foreach (var sortedItem in sorted)
        {
            ClipboardItems.Add(sortedItem);
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        var result = System.Windows.MessageBox.Show(
            AppConstants.Messages.ClearHistoryConfirm,
            AppConstants.Messages.ClearHistoryTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _databaseService.ClearHistory(keepPinned: true);
            LoadItems();
        }
    }

    [RelayCommand]
    private void ClearOlderThanDay()
    {
        var result = System.Windows.MessageBox.Show(
            AppConstants.Messages.ClearOlderThanDayConfirm,
            AppConstants.Messages.ClearOlderThanDayTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _databaseService.ClearItemsOlderThan(1, keepPinned: true);
            LoadItems();
        }
    }

    [RelayCommand]
    private void ClearOlderThanWeek()
    {
        var result = System.Windows.MessageBox.Show(
            AppConstants.Messages.ClearOlderThanWeekConfirm,
            AppConstants.Messages.ClearOlderThanWeekTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _databaseService.ClearItemsOlderThan(7, keepPinned: true);
            LoadItems();
        }
    }

    [RelayCommand]
    private void ClearOlderThanMonth()
    {
        var result = System.Windows.MessageBox.Show(
            AppConstants.Messages.ClearOlderThanMonthConfirm,
            AppConstants.Messages.ClearOlderThanMonthTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _databaseService.ClearItemsOlderThan(30, keepPinned: true);
            LoadItems();
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadItems();
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _configService.UpdateConfig(config =>
        {
            config.Hotkey = CurrentHotkey;
            config.MaxHistoryCount = MaxHistoryCount;
            config.AutoStart = AutoStart;
            config.Theme = SelectedTheme;
        });
        _themeService.ApplyTheme(SelectedTheme);
        
        // Re-register the hotkey with the new value
        var result = _hotkeyService.RegisterHotkey(CurrentHotkey);
        if (result != HotkeyRegistrationResult.Success)
        {
            var message = result switch
            {
                HotkeyRegistrationResult.Conflict => AppConstants.Messages.HotkeyConflictMessage(CurrentHotkey),
                HotkeyRegistrationResult.InvalidHotkey => AppConstants.Messages.HotkeyInvalidMessage(CurrentHotkey),
                _ => AppConstants.Messages.HotkeyFailedMessage(CurrentHotkey)
            };
            
            System.Windows.MessageBox.Show(message, AppConstants.Messages.HotkeyConflictTitle, 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
        }
        
        IsSettingsOpen = false;
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _themeService.ApplyTheme(value);
        
        // Force refresh of the selected item's preview by temporarily clearing and restoring it
        // This ensures the FlowDocument is recreated with the new theme colors
        if (SelectedItem != null)
        {
            var currentItem = SelectedItem;
            SelectedItem = null;
            SelectedItem = currentItem;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Reset the debounce timer on each keystroke
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    partial void OnSelectedTypeFilterChanged(string value)
    {
        // Type filter change triggers immediate filtering (debounced as well)
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    private void OnFilterDebounceTimerTick(object? sender, EventArgs e)
    {
        _filterDebounceTimer.Stop();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        // Use cached items instead of querying the database
        var items = _allItemsCache.ToList();
        
        // Apply type filter if selected
        if (!string.IsNullOrWhiteSpace(SelectedTypeFilter))
        {
            items = SelectedTypeFilter switch
            {
                AppConstants.FilterTypes.Text => items.Where(i => i.ContentType == ContentType.Text && i.FormatType == ContentFormatType.PlainText).ToList(),
                AppConstants.FilterTypes.Image => items.Where(i => i.ContentType == ContentType.Image).ToList(),
                AppConstants.FilterTypes.Json => items.Where(i => i.FormatType == ContentFormatType.Json).ToList(),
                AppConstants.FilterTypes.Xml => items.Where(i => i.FormatType == ContentFormatType.Xml).ToList(),
                AppConstants.FilterTypes.Yaml => items.Where(i => i.FormatType == ContentFormatType.Yaml).ToList(),
                AppConstants.FilterTypes.Markdown => items.Where(i => i.FormatType == ContentFormatType.Markdown).ToList(),
                AppConstants.FilterTypes.Html => items.Where(i => i.FormatType == ContentFormatType.Html || i.FormatType == ContentFormatType.HtmlCode).ToList(),
                AppConstants.FilterTypes.Css => items.Where(i => i.FormatType == ContentFormatType.Css).ToList(),
                AppConstants.FilterTypes.CSharp => items.Where(i => i.FormatType == ContentFormatType.CSharp).ToList(),
                AppConstants.FilterTypes.JavaScript => items.Where(i => i.FormatType == ContentFormatType.JavaScript).ToList(),
                AppConstants.FilterTypes.Sql => items.Where(i => i.FormatType == ContentFormatType.Sql).ToList(),
                AppConstants.FilterTypes.Python => items.Where(i => i.FormatType == ContentFormatType.Python).ToList(),
                AppConstants.FilterTypes.Shell => items.Where(i => i.FormatType == ContentFormatType.Bash || i.FormatType == ContentFormatType.PowerShell).ToList(),
                _ => items
            };
        }
        
        // Apply search text filter if present
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            items = items.Where(i => i.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                   (i.PreviewText?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                       .ToList();
        }
        
        ClipboardItems.Clear();
        foreach (var item in items)
        {
            ClipboardItems.Add(item);
        }
        ItemCount = ClipboardItems.Count;
    }

    public void Cleanup()
    {
        _filterDebounceTimer.Stop();
        _clipboardMonitor.ClipboardChanged -= OnClipboardChanged;
    }
}
