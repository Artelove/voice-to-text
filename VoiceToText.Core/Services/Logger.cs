using System;

namespace VoiceToText.Core.Services;

public static class Logger
{
    public static void Info(string message, params object[] args)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] INFO: " + string.Format(message, args));
    }

    public static void Debug(string message, params object[] args)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DEBUG: " + string.Format(message, args));
    }

    public static void Warn(string message, params object[] args)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] WARN: " + string.Format(message, args));
    }

    public static void Error(string message, params object[] args)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR: " + string.Format(message, args));
    }
}