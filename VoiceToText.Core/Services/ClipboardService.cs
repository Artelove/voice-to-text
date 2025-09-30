using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VoiceToText.Core.Services;

public static class ClipboardService
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    /// <summary>
    /// Copies text to clipboard and pastes it into the active window
    /// </summary>
    public static async Task CopyAndPasteTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Logger.Warn("Cannot copy and paste empty text");
            return;
        }

        try
        {
            // Copy text to clipboard using Windows API
            CopyTextToClipboard(text);

            // Small delay to ensure clipboard is updated
            await Task.Delay(100);

            // Simulate Ctrl+V to paste into active window using Windows API
            SimulateCtrlV();

            Logger.Info("Text transcribed and inserted automatically");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to copy and paste text: {0}", ex.Message);
        }
    }

    private static void CopyTextToClipboard(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
            throw new Exception("Failed to open clipboard");

        try
        {
            EmptyClipboard();

            // Allocate global memory for the text
            var bytes = (text.Length + 1) * 2; // Unicode characters are 2 bytes each
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);

            if (hGlobal == IntPtr.Zero)
                throw new Exception("Failed to allocate global memory");

            try
            {
                var pGlobal = GlobalLock(hGlobal);
                if (pGlobal == IntPtr.Zero)
                    throw new Exception("Failed to lock global memory");

                try
                {
                    // Copy the text to the global memory
                    System.Runtime.InteropServices.Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);

                    // Set the clipboard data
                    if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                        throw new Exception("Failed to set clipboard data");

                    // Don't free hGlobal here - Windows clipboard now owns it
                    hGlobal = IntPtr.Zero;
                }
                finally
                {
                    if (pGlobal != IntPtr.Zero)
                        GlobalUnlock(pGlobal);
                }
            }
            finally
            {
                if (hGlobal != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(hGlobal);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static void SimulateCtrlV()
    {
        // Press Ctrl
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        // Press V
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        // Release V
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        // Release Ctrl
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
