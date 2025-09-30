using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using VoiceToText.Core;
using VoiceToText.Core.Services;

namespace VoiceToText.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private const string TrayIconFileName = "nature_zoo_exotic_tropical_jungle_bird_toucan_wildlife_icon_267097.ico";

    private const int HotkeyStartId = 0x1000;
    private const int HotkeyStopId = 0x1001;

    private VoiceToTextManager? _manager;
    private RecordingOverlayWindow? _overlay;
    private MainWindow? _mainWindow;
    private HwndSource? _hotkeySource;
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _menuStartRecording;
    private Forms.ToolStripMenuItem? _menuStopRecording;
    private Icon? _trayIconHandle;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Logger.Info("=== VOICE-TO-TEXT APPLICATION STARTING ===");
        // set ru console
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        base.OnStartup(e);

        Logger.Debug("Loading application settings...");
        var settings = new AppSettings();

        Logger.Info("Initializing VoiceToTextManager...");
        _manager = new VoiceToTextManager(settings);
        await _manager.InitializeAsync(CancellationToken.None);

        Logger.Debug("Creating overlay and main windows...");
        _overlay = new RecordingOverlayWindow();
        _mainWindow = new MainWindow(_manager, _overlay);
        ApplyWindowIcon();

        _mainWindow.SourceInitialized += (_, _) =>
        {
            Logger.Info("Registering global hotkeys...");
            if (_mainWindow != null)
            {
                RegisterHotkeys(_mainWindow);
            }
        };

        _mainWindow.Loaded += (_, _) => _mainWindow.HideToTray();

        Logger.Debug("Initializing system tray icon...");
        InitializeTrayIcon();

        this.MainWindow = _mainWindow;
        _mainWindow.Show();

        Logger.Info("=== APPLICATION INITIALIZED AND READY ===");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Logger.Info("=== APPLICATION SHUTTING DOWN ===");

        Logger.Debug("Disposing system tray icon...");
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _trayIconHandle?.Dispose();
        _trayIconHandle = null;

        if (_hotkeySource != null)
        {
            Logger.Debug("Unregistering global hotkeys...");
            _hotkeySource.RemoveHook(WndProc);
            UnregisterHotKey(_hotkeySource.Handle, HotkeyStartId);
            UnregisterHotKey(_hotkeySource.Handle, HotkeyStopId);
        }

        if (_manager != null)
        {
            Logger.Debug("Disposing VoiceToTextManager...");
            await _manager.DisposeAsync();
        }

        Logger.Info("=== APPLICATION SHUTDOWN COMPLETE ===");
        base.OnExit(e);
    }

    private void InitializeTrayIcon()
    {
        if (_mainWindow == null)
        {
            return;
        }

        _notifyIcon = new Forms.NotifyIcon
        {
            Visible = true,
            Text = "Voice to Text"
        };

        _notifyIcon.Icon = LoadTrayIcon();

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindowFromTray();

        var contextMenu = new Forms.ContextMenuStrip();
        var openItem = new Forms.ToolStripMenuItem("Открыть окно", null, (_, _) => ShowMainWindowFromTray());
        _menuStartRecording = new Forms.ToolStripMenuItem("Начать запись", null, async (_, _) => await StartRecordingFromTrayAsync());
        _menuStopRecording = new Forms.ToolStripMenuItem("Остановить запись", null, async (_, _) => await StopRecordingFromTrayAsync());
        var exitItem = new Forms.ToolStripMenuItem("Выход", null, (_, _) => ExitApplication());

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_menuStartRecording);
        contextMenu.Items.Add(_menuStopRecording);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);
        contextMenu.Opening += (_, _) => UpdateTrayMenuItems();

        _notifyIcon.ContextMenuStrip = contextMenu;

        UpdateTrayMenuItems();
    }

    private void ShowMainWindowFromTray()
    {
        if (_mainWindow == null)
        {
            return;
        }

        Logger.Info("Restoring main window from tray icon interaction");
        _mainWindow.RestoreFromTray();
    }

    private async Task StartRecordingFromTrayAsync()
    {
        Logger.Info("Tray menu requested start recording");
        await StartRecordingAsync();
        UpdateTrayMenuItems();
    }

    private async Task StopRecordingFromTrayAsync()
    {
        Logger.Info("Tray menu requested stop recording");
        await StopRecordingAsync();
        UpdateTrayMenuItems();
    }

    private void UpdateTrayMenuItems()
    {
        if (_mainWindow == null)
        {
            return;
        }

        var isRecording = _mainWindow.IsRecording;
        if (_menuStartRecording != null)
        {
            _menuStartRecording.Enabled = !isRecording;
        }

        if (_menuStopRecording != null)
        {
            _menuStopRecording.Enabled = isRecording;
        }
    }

    private Icon LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, TrayIconFileName);

        if (File.Exists(iconPath))
        {
            try
            {
                _trayIconHandle?.Dispose();
                _trayIconHandle = new Icon(iconPath);
                Logger.Debug("Loaded tray icon from {0}", iconPath);
                return _trayIconHandle;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load tray icon '{0}': {1}", iconPath, ex.Message);
            }
        }
        else
        {
            Logger.Warn("Tray icon file not found at {0}; fallback to default", iconPath);
        }

        return SystemIcons.Application;
    }

    private void ApplyWindowIcon()
    {
        if (_mainWindow == null)
        {
            return;
        }

        try
        {
            var iconUri = new Uri($"pack://siteoforigin:,,,/{TrayIconFileName}", UriKind.Absolute);
            var frame = BitmapFrame.Create(iconUri);
            _mainWindow.Icon = frame;
            Logger.Debug("Applied main window icon from {0}", iconUri);
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not apply main window icon: {0}", ex.Message);
        }
    }

    private void ExitApplication()
    {
        Logger.Info("Exit requested from system tray");
        if (_mainWindow != null)
        {
            _mainWindow.PrepareForShutdown();
            _mainWindow.Close();
        }

        System.Windows.Application.Current.Shutdown();
    }

    private void RegisterHotkeys(Window window)
    {
        Logger.Debug("Setting up window interop helper...");
        var helper = new WindowInteropHelper(window);
        helper.EnsureHandle();
        _hotkeySource = HwndSource.FromHwnd(helper.Handle);
        _hotkeySource?.AddHook(WndProc);

        Logger.Debug("Registering hotkey: Ctrl+Alt+Q (Start Recording)");
        RegisterHotKey(helper.Handle, HotkeyStartId, MOD_CONTROL | MOD_ALT, KeyInterop.VirtualKeyFromKey(Key.Q));

        Logger.Debug("Registering hotkey: Ctrl+Alt+W (Stop Recording)");
        RegisterHotKey(helper.Handle, HotkeyStopId, MOD_CONTROL | MOD_ALT, KeyInterop.VirtualKeyFromKey(Key.W));

        Logger.Info("Global hotkeys registered successfully");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && _manager != null && _overlay != null && _mainWindow != null)
        {
            var hotkeyId = wParam.ToInt32();
            if (hotkeyId == HotkeyStartId)
            {
                if (!_mainWindow.IsRecording)
                {
                    Logger.Info("🔥 HOTKEY START PRESSED: Ctrl+Alt+Q");
                    _ = StartRecordingAsync();
                }
                else
                {
                    Logger.Warn("HOTKEY START ignored: recording already active");
                }
                handled = true;
            }
            else if (hotkeyId == HotkeyStopId)
            {
                if (_mainWindow.IsRecording)
                {
                    Logger.Info("🛑 HOTKEY STOP PRESSED: Ctrl+Alt+W");
                    _ = StopRecordingAsync();
                }
                else
                {
                    Logger.Warn("HOTKEY STOP ignored: no active recording");
                }
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private async Task StartRecordingAsync()
    {
        if (_overlay == null || _manager == null || _mainWindow == null)
        {
            Logger.Error("Cannot start recording: one or more components are null");
            return;
        }

        Logger.Debug("Showing recording overlay near cursor...");
        _overlay.Dispatcher.Invoke(() => _overlay.ShowNearCursor());

        Logger.Debug("Starting recording process...");
        await _manager.StartRecordingAsync(CancellationToken.None);

        Logger.Debug("Updating UI state to recording...");
        _mainWindow.SetRecordingState(true, "Recording...");
        Logger.Info("Recording session started successfully");

        UpdateTrayMenuItems();
    }

    private async Task StopRecordingAsync()
    {
        if (_overlay == null || _manager == null || _mainWindow == null)
        {
            Logger.Error("Cannot stop recording: one or more components are null");
            return;
        }

        Logger.Debug("Hiding recording overlay...");
        _overlay.Dispatcher.Invoke(() => _overlay.HideOverlay());

        Logger.Debug("Stopping recording and transcribing...");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var text = await _manager.StopAndTranscribeAsync(cts.Token);

            Logger.Debug("Updating UI with transcription result...");
            _mainWindow.ShowTranscription(text);
            _mainWindow.SetRecordingState(false);
            Logger.Info("Recording session completed, transcription shown to user");
            UpdateTrayMenuItems();
        }
        catch (Exception ex)
        {
            Logger.Error("Error during transcription: {0}", ex.Message);
            _mainWindow.ShowTranscription($"Error: {ex.Message}");
            _mainWindow.SetRecordingState(false);
            UpdateTrayMenuItems();
        }
    }

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

