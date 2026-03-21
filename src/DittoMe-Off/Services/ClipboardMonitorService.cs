using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DittoMeOff.Models;

namespace DittoMeOff.Services;

public class ClipboardMonitorService : IDisposable
{
    private System.Windows.Threading.DispatcherTimer? _timer;
    private readonly ConfigService _configService;
    private readonly DatabaseService _databaseService;
    private IntPtr _nextClipboardSequenceNumber;
    private bool _isMonitoring;

    public event EventHandler<ClipboardItem>? ClipboardChanged;

    public ClipboardMonitorService(ConfigService configService, DatabaseService databaseService)
    {
        _configService = configService;
        _databaseService = databaseService;
    }

    public void Start()
    {
        if (_isMonitoring) return;

        _nextClipboardSequenceNumber = GetClipboardSequenceNumber();
        
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_configService.Config.ClipboardPollInterval)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _isMonitoring = true;
    }

    public void Stop()
    {
        _timer?.Stop();
        _isMonitoring = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var currentSequence = GetClipboardSequenceNumber();
            if (currentSequence != _nextClipboardSequenceNumber)
            {
                _nextClipboardSequenceNumber = currentSequence;
                var item = CaptureClipboard();
                if (item != null)
                {
                    EnforceHistoryLimit();
                    var id = _databaseService.InsertItem(item);
                    item.Id = id;
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ClipboardChanged?.Invoke(this, item);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error monitoring clipboard: {ex.Message}");
        }
    }

    private ClipboardItem? CaptureClipboard()
    {
        ClipboardItem? item = null;
        
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                string? appSource = GetSourceAppName();
                
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Detect the content format (JSON, XML, code, etc.)
                        var formatType = ContentFormatDetector.Detect(text);
                        
                        item = new ClipboardItem
                        {
                            Content = text,
                            ContentType = ContentType.Text,
                            FormatType = formatType,
                            Timestamp = DateTime.Now,
                            PreviewText = text.Length > 5000 ? text.Substring(0, 5000) : text,
                            Size = Encoding.UTF8.GetByteCount(text),
                            AppSource = appSource
                        };
                    }
                }
                else if (Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();
                    if (image != null)
                    {
                        var imageData = BitmapSourceToBytes(image);
                        if (imageData != null && imageData.Length <= _configService.Config.MaxItemSize)
                        {
                            item = new ClipboardItem
                            {
                                Content = $"Image {image.PixelWidth}x{image.PixelHeight}",
                                ContentType = ContentType.Image,
                                Timestamp = DateTime.Now,
                                PreviewText = $"Image {image.PixelWidth}x{image.PixelHeight}",
                                Size = imageData.Length,
                                ImageData = imageData,
                                AppSource = appSource
                            };
                        }
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files.Count > 0)
                    {
                        var fileList = string.Join("\n", files.Cast<string>());
                        item = new ClipboardItem
                        {
                            Content = fileList,
                            ContentType = ContentType.File,
                            Timestamp = DateTime.Now,
                            PreviewText = files.Count == 1 ? Path.GetFileName(files[0]) : $"{files.Count} files",
                            Size = Encoding.UTF8.GetByteCount(fileList),
                            AppSource = appSource
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturing clipboard: {ex.Message}");
            }
        });
        
        return item;
    }

    private byte[]? BitmapSourceToBytes(BitmapSource source)
    {
        try
        {
            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private void EnforceHistoryLimit()
    {
        var count = _databaseService.GetItemCount();
        if (count > _configService.Config.MaxHistoryCount)
        {
            _databaseService.ClearHistory(keepPinned: true);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private string? GetSourceAppName()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0)
                return null;

            var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardSequenceNumber(IntPtr hWnd);

    private static IntPtr GetClipboardSequenceNumber()
    {
        try
        {
            return GetClipboardSequenceNumber(IntPtr.Zero);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
