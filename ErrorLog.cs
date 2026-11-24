using System.IO;

namespace LavistechFileToolkit;

/// <summary>Appends errors to %AppData%\LavistechFileToolkit\error.log. Never throws.</summary>
public static class ErrorLog
{
    const long MaxBytes = 1_000_000;
    static readonly object Gate = new();

    public static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LavistechFileToolkit");

    public static readonly string FilePath = Path.Combine(Folder, "error.log");

    /// <summary>Raised on the calling thread whenever an error is logged.</summary>
    public static event Action<string, Exception>? Reported;

    public static void Write(string context, Exception ex)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxBytes) File.Delete(FilePath);
                File.AppendAllText(FilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] thread {Environment.CurrentManagedThreadId} - {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never take the app down.
        }

        try { Reported?.Invoke(context, ex); }
        catch { /* listeners must not break the caller */ }
    }
}
