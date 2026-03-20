using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DittoMeOff.Models;
using DittoMeOff.Services;

namespace DittoMeOff.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly ConfigService _configService;
    private readonly ClipboardMonitorService _clipboardMonitor;
    private readonly ThemeService _themeService;
    private readonly HotkeyService _hotkeyService;

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
    private string _currentHotkey = "Ctrl+Shift+V";

    [ObservableProperty]
    private int _maxHistoryCount = 100;

    [ObservableProperty]
    private bool _autoStart;

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.Light;

    [ObservableProperty]
    private Dictionary<AppTheme, string> _themeOptions;

    public MainViewModel(DatabaseService databaseService, ConfigService configService, ClipboardMonitorService clipboardMonitor, ThemeService themeService, HotkeyService hotkeyService)
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
            ClipboardItems.Insert(0, item);
            
            // Remove duplicates from old list
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
        
        _databaseService.DeleteItem(item.Id);
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
            "Are you sure you want to clear all unpinned clipboard history? Pinned items will be kept.",
            "Clear History",
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
            "Are you sure you want to clear all clipboard history older than 1 day? Pinned items will be kept.",
            "Clear Older Than 1 Day",
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
            "Are you sure you want to clear all clipboard history older than 1 week? Pinned items will be kept.",
            "Clear Older Than 1 Week",
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
            "Are you sure you want to clear all clipboard history older than 1 month? Pinned items will be kept.",
            "Clear Older Than 1 Month",
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
        _hotkeyService.RegisterHotkey(CurrentHotkey);
        
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
        ApplyFilters();
    }

    partial void OnSelectedTypeFilterChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var items = _databaseService.GetItems(_configService.Config.MaxHistoryCount);
        
        // Apply type filter if selected
        if (!string.IsNullOrWhiteSpace(SelectedTypeFilter))
        {
            items = SelectedTypeFilter switch
            {
                "Text" => items.Where(i => i.ContentType == ContentType.Text && i.FormatType == ContentFormatType.PlainText).ToList(),
                "Image" => items.Where(i => i.ContentType == ContentType.Image).ToList(),
                "Json" => items.Where(i => i.FormatType == ContentFormatType.Json).ToList(),
                "Xml" => items.Where(i => i.FormatType == ContentFormatType.Xml).ToList(),
                "Html" => items.Where(i => i.FormatType == ContentFormatType.Html || i.FormatType == ContentFormatType.HtmlCode).ToList(),
                "CSharp" => items.Where(i => i.FormatType == ContentFormatType.CSharp).ToList(),
                "JavaScript" => items.Where(i => i.FormatType == ContentFormatType.JavaScript).ToList(),
                "Sql" => items.Where(i => i.FormatType == ContentFormatType.Sql).ToList(),
                "Python" => items.Where(i => i.FormatType == ContentFormatType.Python).ToList(),
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
        _clipboardMonitor.ClipboardChanged -= OnClipboardChanged;
    }
}
