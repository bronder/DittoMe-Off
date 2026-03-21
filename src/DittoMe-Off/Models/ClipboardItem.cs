using System.IO;
using System.Windows.Media.Imaging;
using DittoMeOff.Services;

namespace DittoMeOff.Models;

public class ClipboardItem
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public ContentFormatType FormatType { get; set; } = ContentFormatType.PlainText;
    public DateTime Timestamp { get; set; }
    public bool IsPinned { get; set; }
    public string? AppSource { get; set; }
    public long Size { get; set; }
    public string? PreviewText { get; set; }
    public byte[]? ImageData { get; set; }
    
    // Cached image source for preview
    private BitmapSource? _imageSource;
    public BitmapSource? ImageSource
    {
        get
        {
            if (ImageData == null) return null;
            if (_imageSource != null) return _imageSource;
            
            try
            {
                using var stream = new MemoryStream(ImageData);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                _imageSource = bitmap;
                return _imageSource;
            }
            catch
            {
                return null;
            }
        }
    }
    
    public string DisplayText => ContentType switch
    {
        ContentType.Image => PreviewText ?? "[Image]",
        ContentType.File => Content,
        _ => PreviewText ?? Content
    };
    
    public string TimestampDisplay => Timestamp.ToString("HH:mm");
    
    public string TimeGroupHeader
    {
        get
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            
            if (Timestamp.Date == today)
                return "Today";
            if (Timestamp.Date == yesterday)
                return "Yesterday";
            if (Timestamp.Date > today.AddDays(-7))
                return "This Week";
            if (Timestamp.Date > today.AddDays(-30))
                return "This Month";
            return "Older";
        }
    }
    
    public string ContentTypeIcon => ContentType switch
    {
        ContentType.Text => "\uE8C1",    // Segoe MDL2 Assets icon for text
        ContentType.Image => "\uE91B",   // Photo icon
        ContentType.File => "\uE8B7",    // File icon
        ContentType.Html => "\uE12B",   // Code icon
        _ => "\uE8C1"
    };
    
    // Full timestamp for tooltip
    public string FullTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    
    // Format badge properties for display
    public bool HasFormatBadge => FormatType != ContentFormatType.PlainText && 
                                   (ContentType == ContentType.Text || ContentType == ContentType.Html);
    
    public string FormatBadgeText => ContentFormatDetector.GetFormatDisplayName(FormatType);
    
    public string FormatBadgeIcon => ContentFormatDetector.GetFormatIcon(FormatType);
    
    // Size display formatting
    public string SizeDisplay
    {
        get
        {
            if (Size < 1024) return $"{Size} B";
            if (Size < 1024 * 1024) return $"{Size / 1024.0:F1} KB";
            return $"{Size / (1024.0 * 1024.0):F1} MB";
        }
    }
    
    // App source display (truncated to fit)
    public string AppSourceDisplay => string.IsNullOrEmpty(AppSource) ? "Unknown" : 
        AppSource.Length > 30 ? AppSource.Substring(0, 27) + "..." : AppSource;
    
    // Date/time display for header
    public string DateTimeDisplay => Timestamp.ToString("yyyy-MM-dd HH:mm");
}
