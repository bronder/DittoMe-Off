using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DittoMeOff.Models;
using NLog;

namespace DittoMeOff.Services;

public class ClipboardMonitorService : IClipboardMonitorService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private System.Windows.Threading.DispatcherTimer? _timer;
    private readonly IConfigService _configService;
    private readonly IDatabaseService _databaseService;
    private IntPtr _nextClipboardSequenceNumber;
    private bool _isMonitoring;

    public event EventHandler<ClipboardItem>? ClipboardChanged;

    public ClipboardMonitorService(IConfigService configService, IDatabaseService databaseService)
    {
        _configService = configService;
        _databaseService = databaseService;
    }

    public void Start()
    {
        if (_isMonitoring)
        {
            _logger.Debug("Start called but already monitoring, skipping");
            return;
        }

        _nextClipboardSequenceNumber = NativeMethods.GetClipboardSequenceNumber();
        
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_configService.Config.ClipboardPollInterval)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _isMonitoring = true;
        _logger.Info("Clipboard monitoring started with poll interval: {PollInterval}ms", _configService.Config.ClipboardPollInterval);
    }

    public void Stop()
    {
        if (!_isMonitoring)
        {
            _logger.Debug("Stop called but not monitoring, skipping");
            return;
        }

        _timer?.Stop();
        _isMonitoring = false;
        _logger.Info("Clipboard monitoring stopped");
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var currentSequence = NativeMethods.GetClipboardSequenceNumber();
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
            _logger.Error(ex, "Error monitoring clipboard");
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
                            PreviewText = text.Length > AppConstants.PreviewTextMaxLength ? text.Substring(0, AppConstants.PreviewTextMaxLength) : text,
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
                _logger.Error(ex, "Error capturing clipboard");
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
        catch (Exception ex)
        {
            _logger.Error(ex, "Error converting BitmapSource to bytes");
            return null;
        }
    }

    private void EnforceHistoryLimit()
    {
        var count = _databaseService.GetItemCount();
        if (count > _configService.Config.MaxHistoryCount)
        {
            // Only delete the oldest excess items, not all non-pinned items
            _databaseService.DeleteOldestExcessItems(_configService.Config.MaxHistoryCount, keepPinned: true);
        }
    }

    private string? GetSourceAppName()
    {
        try
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0)
                return null;

            // Use TryGetById to avoid exceptions when process has exited
            if (System.Diagnostics.Process.GetProcessById((int)processId) is { } process)
            {
                return process.ProcessName;
            }
        }
        catch (InvalidOperationException)
        {
            // Process has exited between checking and accessing it
        }
        catch (ArgumentException)
        {
            // Invalid process ID
        }
        return null;
    }

    public void Dispose()
    {
        Stop();
    }
}
