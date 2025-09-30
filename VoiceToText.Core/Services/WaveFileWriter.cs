using System;
using System.IO;

namespace VoiceToText.Core.Services;

public static class WaveExportService
{
    public static void WriteToStream(float[] audioBuffer, int sampleRate, int channels, Stream destination)
    {
        // WAV header
        const int bitsPerSample = 16;
        const int bytesPerSample = bitsPerSample / 8;
        int dataLength = audioBuffer.Length * bytesPerSample;
        int fileLength = 36 + dataLength;

        using var writer = new BinaryWriter(destination, System.Text.Encoding.ASCII, leaveOpen: true);

        // RIFF header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(fileLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // Format chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // Chunk size
        writer.Write((short)1); // Audio format (PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bytesPerSample); // Byte rate
        writer.Write((short)(channels * bytesPerSample)); // Block align
        writer.Write((short)bitsPerSample);

        // Data chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        // Audio data
        foreach (var sample in audioBuffer)
        {
            // Convert float [-1, 1] to int16
            var intSample = (short)(sample * 32767);
            writer.Write(intSample);
        }
    }
}
