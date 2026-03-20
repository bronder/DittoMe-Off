using System.Windows;
using System.Windows.Media;
using DittoMeOff.Models;

namespace DittoMeOff.Services;

public class ThemeService
{
    private readonly ConfigService _configService;
    private ResourceDictionary? _currentThemeDictionary;
    private readonly string _themesFolder;

    public ThemeService(ConfigService configService)
    {
        _configService = configService;
        _themesFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
    }

    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        try
        {
            // Remove current theme dictionary if exists
            if (_currentThemeDictionary != null)
            {
                app.Resources.MergedDictionaries.Remove(_currentThemeDictionary);
            }

            // Load the new theme
            var themeFileName = $"{theme}.xaml";
            var themePath = System.IO.Path.Combine(_themesFolder, themeFileName);
            
            System.Diagnostics.Debug.WriteLine($"[ThemeService] Attempting to load theme: {theme}");
            System.Diagnostics.Debug.WriteLine($"[ThemeService] Theme path: {themePath}");
            System.Diagnostics.Debug.WriteLine($"[ThemeService] File exists: {System.IO.File.Exists(themePath)}");

            if (System.IO.File.Exists(themePath))
            {
                var uri = new Uri(themePath, UriKind.Absolute);
                System.Diagnostics.Debug.WriteLine($"[ThemeService] Loading from URI: {uri}");
                var newTheme = new ResourceDictionary { Source = uri };
                
                app.Resources.MergedDictionaries.Add(newTheme);
                _currentThemeDictionary = newTheme;
                System.Diagnostics.Debug.WriteLine($"[ThemeService] Theme loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeService] File not found, applying fallback colors");
                // Fallback to embedded colors if theme file not found
                ApplyFallbackColors(theme);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            ApplyFallbackColors(theme);
        }
    }

    public void LoadSavedTheme()
    {
        ApplyTheme(_configService.Config.Theme);
    }

    private void ApplyFallbackColors(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        var colors = GetFallbackColors(theme);
        
        // Update application resources with fallback colors
        // Using the same brush names as defined in theme XAML files
        app.Resources["BackgroundBrush"] = colors.background;
        app.Resources["CardBrush"] = colors.surface;
        app.Resources["HeaderBrush"] = colors.surfaceVariant;
        app.Resources["AccentBrush"] = colors.accent;
        app.Resources["AccentHoverBrush"] = colors.accent;
        app.Resources["AccentPressedBrush"] = colors.accent;
        app.Resources["TextBrush"] = colors.textPrimary;
        app.Resources["SecondaryTextBrush"] = colors.textSecondary;
        app.Resources["BorderBrush"] = colors.border;
        app.Resources["BorderFocusBrush"] = colors.border;
        
        // Preview panel brushes
        app.Resources["PreviewBackgroundBrush"] = colors.previewBackground;
        app.Resources["PreviewHeaderBrush"] = colors.surfaceVariant;
        app.Resources["PreviewTextBrush"] = colors.textPrimary;
        app.Resources["PreviewSecondaryTextBrush"] = colors.textSecondary;
        app.Resources["PreviewCodeKeywordBrush"] = colors.previewKeyword;
        app.Resources["PreviewCodeStringBrush"] = colors.previewString;
        app.Resources["PreviewCodeCommentBrush"] = colors.previewComment;
        app.Resources["PreviewCodeNumberBrush"] = colors.previewNumber;
        app.Resources["PreviewCodeKeyBrush"] = colors.previewKey;
        
        // Badge brushes
        app.Resources["ValidBadgeBrush"] = colors.validBadge;
        app.Resources["InvalidBadgeBrush"] = colors.invalidBadge;
        app.Resources["InfoBadgeBrush"] = colors.infoBadge;
    }

    private (SolidColorBrush background, SolidColorBrush surface, SolidColorBrush surfaceVariant,
            SolidColorBrush accent, SolidColorBrush textPrimary, SolidColorBrush textSecondary,
            SolidColorBrush border, SolidColorBrush previewBackground, SolidColorBrush previewKeyword,
            SolidColorBrush previewString, SolidColorBrush previewComment, SolidColorBrush previewNumber,
            SolidColorBrush previewKey, SolidColorBrush validBadge, SolidColorBrush invalidBadge,
            SolidColorBrush infoBadge) GetFallbackColors(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Dark or AppTheme.Dracula or AppTheme.Gruvbox or AppTheme.Monokai or 
            AppTheme.Nord or AppTheme.Synthwave or AppTheme.TokyoNight => (
                new SolidColorBrush(Color.FromRgb(30, 30, 30)),      // background
                new SolidColorBrush(Color.FromRgb(37, 37, 37)),      // surface (CardBrush)
                new SolidColorBrush(Color.FromRgb(45, 45, 45)),      // surfaceVariant (HeaderBrush)
                new SolidColorBrush(Color.FromRgb(0, 120, 212)),    // accent
                new SolidColorBrush(Color.FromRgb(255, 255, 255)),  // textPrimary (TextBrush)
                new SolidColorBrush(Color.FromRgb(136, 136, 136)),  // textSecondary (SecondaryTextBrush)
                new SolidColorBrush(Color.FromRgb(62, 62, 62)),     // border
                new SolidColorBrush(Color.FromRgb(30, 30, 30)),     // previewBackground
                new SolidColorBrush(Color.FromRgb(86, 156, 214)),   // previewKeyword (VS Code dark style)
                new SolidColorBrush(Color.FromRgb(206, 145, 120)),  // previewString
                new SolidColorBrush(Color.FromRgb(106, 153, 85)),  // previewComment
                new SolidColorBrush(Color.FromRgb(181, 206, 168)), // previewNumber
                new SolidColorBrush(Color.FromRgb(156, 220, 254)), // previewKey
                new SolidColorBrush(Color.FromRgb(75, 175, 80)),   // validBadge (green)
                new SolidColorBrush(Color.FromRgb(220, 80, 80)),   // invalidBadge (red)
                new SolidColorBrush(Color.FromRgb(100, 100, 100))  // infoBadge (gray)
            ),
            _ => (
                new SolidColorBrush(Color.FromRgb(255, 255, 255)), // background
                new SolidColorBrush(Color.FromRgb(245, 245, 245)), // surface (CardBrush)
                new SolidColorBrush(Color.FromRgb(232, 232, 232)), // surfaceVariant (HeaderBrush)
                new SolidColorBrush(Color.FromRgb(0, 120, 212)),   // accent
                new SolidColorBrush(Color.FromRgb(30, 30, 30)),    // textPrimary (TextBrush)
                new SolidColorBrush(Color.FromRgb(102, 102, 102)), // textSecondary (SecondaryTextBrush)
                new SolidColorBrush(Color.FromRgb(221, 221, 221)), // border
                new SolidColorBrush(Color.FromRgb(255, 255, 255)), // previewBackground
                new SolidColorBrush(Color.FromRgb(0, 0, 255)),    // previewKeyword (VS Code light style)
                new SolidColorBrush(Color.FromRgb(163, 21, 21)),  // previewString
                new SolidColorBrush(Color.FromRgb(0, 128, 0)),     // previewComment
                new SolidColorBrush(Color.FromRgb(9, 134, 88)),    // previewNumber
                new SolidColorBrush(Color.FromRgb(4, 81, 165)),    // previewKey
                new SolidColorBrush(Color.FromRgb(46, 125, 50)),   // validBadge (green)
                new SolidColorBrush(Color.FromRgb(211, 47, 47)),  // invalidBadge (red)
                new SolidColorBrush(Color.FromRgb(96, 125, 139))   // infoBadge (blue-gray)
            )
        };
    }
}
