namespace DittoMeOff.Models;

public class AppConfig
{
    public int MaxHistoryCount { get; set; } = 100;
    public string Hotkey { get; set; } = "Ctrl+Shift+V";
    public bool AutoStart { get; set; } = false;
    public AppTheme Theme { get; set; } = AppTheme.Light;
    public long MaxItemSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public List<string> ExcludedApps { get; set; } = new();
    public int ClipboardPollInterval { get; set; } = 500; // ms
    public double WindowWidth { get; set; } = 450;
    public double WindowHeight { get; set; } = 600;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public double SplitterPosition { get; set; } = 300; // Pixel width of list column
}
