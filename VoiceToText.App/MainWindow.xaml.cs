using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using VoiceToText.Core;
using VoiceToText.Core.Services;

namespace VoiceToText.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly VoiceToTextManager _manager;
    private readonly RecordingOverlayWindow _overlay;
    private bool _isRecording;
    private bool _allowClosing;

    public MainWindow(VoiceToTextManager manager, RecordingOverlayWindow overlay)
    {
        InitializeComponent();
        _manager = manager;
        _overlay = overlay;
        Logger.Debug("MainWindow initialized with manager and overlay");

        StateChanged += OnStateChanged;
        Closing += OnClosing;
    }

    public bool IsRecording => _isRecording;

    public void HideToTray()
    {
        Logger.Debug("Hiding main window to system tray");
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(HideToTray);
            return;
        }

        ShowInTaskbar = false;
        if (WindowState != WindowState.Minimized)
        {
            WindowState = WindowState.Minimized;
        }
        Hide();
    }

    public void RestoreFromTray()
    {
        Logger.Debug("Restoring main window from system tray");
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(RestoreFromTray);
            return;
        }

        ShowInTaskbar = true;
        if (!IsVisible)
        {
            Show();
        }
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    public void PrepareForShutdown()
    {
        Logger.Debug("Allowing main window to close for application shutdown");
        _allowClosing = true;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && !_allowClosing)
        {
            HideToTray();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClosing)
        {
            return;
        }

        Logger.Debug("Intercepting window close to hide in tray");
        e.Cancel = true;
        HideToTray();
    }

    public void SetRecordingState(bool isRecording, string? statusText = null)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetRecordingState(isRecording, statusText));
            return;
        }

        Logger.Debug("Setting recording state: {0}, status: {1}", isRecording, statusText ?? "null");
        _isRecording = isRecording;
        StartButton.IsEnabled = !isRecording;
        StopButton.IsEnabled = isRecording;

        if (statusText != null)
        {
            OutputText.Text = statusText;
        }
    }

    public void ShowTranscription(string text)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ShowTranscription(text));
            return;
        }

        Logger.Debug("Showing transcription result, length: {0} characters", text.Length);
        OutputText.Text = text;
    }

    private async void OnStartClicked(object sender, RoutedEventArgs e)
    {
        Logger.Info("Start button clicked");
        if (_isRecording)
        {
            Logger.Warn("Start button clicked while already recording, ignoring");
            return;
        }

        Logger.Debug("Showing overlay and starting recording...");
        _overlay.ShowNearCursor();
        await _manager.StartRecordingAsync(CancellationToken.None);
        SetRecordingState(true, "Recording...");
        Logger.Info("Recording started via UI button");
    }

    private async void OnStopClicked(object sender, RoutedEventArgs e)
    {
        Logger.Info("Stop button clicked");
        if (!_isRecording)
        {
            Logger.Warn("Stop button clicked while not recording, ignoring");
            return;
        }

        Logger.Debug("Hiding overlay and stopping recording...");
        _overlay.HideOverlay();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var text = await _manager.StopAndTranscribeAsync(cts.Token);
            ShowTranscription(text);
            SetRecordingState(false);
            Logger.Info("Recording stopped via UI button");
        }
        catch (Exception ex)
        {
            Logger.Error("Error during transcription: {0}", ex.Message);
            ShowTranscription($"Error: {ex.Message}");
            SetRecordingState(false);
        }
    }
}