using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace VoiceToText.App;

public partial class RecordingOverlayWindow : Window
{
    public RecordingOverlayWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.Manual;
        Hide();
    }

    public void ShowNearCursor()
    {
        if (!GetCursorPos(out var cursor))
        {
            return;
        }

        var x = cursor.X + 16;
        var y = cursor.Y - (Height / 2);

        Left = x;
        Top = y < 0 ? 0 : y;

        if (!IsVisible)
        {
            Show();
        }
        else
        {
            ActivatePreview();
        }
    }

    public void HideOverlay()
    {
        if (IsVisible)
        {
            Hide();
        }
    }

    private void ActivatePreview()
    {
        Topmost = false;
        Topmost = true;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}

