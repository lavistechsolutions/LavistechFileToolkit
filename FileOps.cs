using System.IO;
using System.Text;
using System.Text.Json;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using SkiaSharp;

namespace LavistechFileToolkit;

static class FileOps
{
    public static readonly string UndoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LavistechFileToolkit", "rename-undo.json");

    public static string ExpandTemplate(string template, string file, int number, DateTime when)
    {
        string name = Path.GetFileNameWithoutExtension(file);
        string ext = Path.GetExtension(file);
        string parent = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(file)) ?? "") ?? "";
        bool hasExt = template.Contains("{ext}", StringComparison.OrdinalIgnoreCase);
        string result = template
            .Replace("{name}", name, StringComparison.OrdinalIgnoreCase)
            .Replace("{ext}", ext, StringComparison.OrdinalIgnoreCase)
            .Replace("{n}", number.ToString("D3"), StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", when.ToString("yyyyMMdd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{parent}", parent, StringComparison.OrdinalIgnoreCase);
        foreach (char c in Path.GetInvalidFileNameChars()) result = result.Replace(c, '_');
        if (!hasExt) result += ext;
        if (string.IsNullOrWhiteSpace(result.Trim('.', '_'))) throw new IOException("Template produced an empty file name.");
        return result;
    }

    public static void SaveUndo(IEnumerable<(string Old, string New)> plan)
    {
        var steps = plan.Where(p => !string.Equals(p.Old, p.New, StringComparison.OrdinalIgnoreCase))
            .Select(p => new RenameStep(p.Old, p.New)).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(UndoPath)!);
        File.WriteAllText(UndoPath, JsonSerializer.Serialize(steps));
    }

    public static string UndoLast()
    {
        if (!File.Exists(UndoPath)) return "Nothing to undo.";
        var steps = JsonSerializer.Deserialize<List<RenameStep>>(File.ReadAllText(UndoPath)) ?? new();
        var sb = new StringBuilder();
        int done = 0;
        foreach (var step in steps)
        {
            try
            {
                if (!File.Exists(step.To)) { sb.AppendLine($"Missing, skipped: {step.To}"); continue; }
                if (File.Exists(step.From)) { sb.AppendLine($"Original name is taken, skipped: {step.From}"); continue; }
                File.Move(step.To, step.From);
                sb.AppendLine($"{Path.GetFileName(step.To)}  →  {Path.GetFileName(step.From)}");
                done++;
            }
            catch (Exception ex) { sb.AppendLine($"FAILED {step.To}: {ex.Message}"); }
        }
        File.Delete(UndoPath);
        sb.Insert(0, $"Restored {done} of {steps.Count} file name(s).{Environment.NewLine}");
        return sb.ToString();
    }

    public static void Recycle(string path) =>
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
            path,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

    public sealed record SyncChange(string Relative, string Action, string Source, string Dest);

    public static List<SyncChange> PlanSync(string source, string dest, bool removeExtras)
    {
        string srcRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
        string dstRoot = Path.GetFullPath(dest).TrimEnd(Path.DirectorySeparatorChar);
        if (srcRoot.Equals(dstRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("Source and destination are the same folder.");
        if (IsInside(srcRoot, dstRoot) || IsInside(dstRoot, srcRoot)) throw new IOException("One folder is inside the other.");
        var left = FilesUnder(srcRoot);
        var right = FilesUnder(dstRoot);
        var plan = new List<SyncChange>();
        foreach (var pair in left)
        {
            string target = Path.Combine(dstRoot, pair.Key);
            if (!right.TryGetValue(pair.Key, out string? existing)) { plan.Add(new(pair.Key, "copy", pair.Value, target)); continue; }
            var a = new FileInfo(pair.Value);
            var b = new FileInfo(existing);
            if (a.Length != b.Length || a.LastWriteTimeUtc > b.LastWriteTimeUtc.AddSeconds(2))
                plan.Add(new(pair.Key, "copy", pair.Value, target));
        }
        if (removeExtras)
            foreach (var pair in right)
                if (!left.ContainsKey(pair.Key)) plan.Add(new(pair.Key, "delete", "", pair.Value));
        return plan;
    }

    public static string ApplySync(IReadOnlyList<SyncChange> plan)
    {
        int copied = 0, removed = 0;
        var sb = new StringBuilder();
        foreach (var change in plan)
        {
            try
            {
                if (change.Action == "copy")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(change.Dest)!);
                    File.Copy(change.Source, change.Dest, true);
                    copied++;
                    sb.AppendLine($"COPY {change.Relative}");
                }
                else
                {
                    Recycle(change.Dest);
                    removed++;
                    sb.AppendLine($"RECYCLE {change.Relative}");
                }
            }
            catch (Exception ex) { sb.AppendLine($"FAILED {change.Relative}: {ex.Message}"); }
        }
        sb.Insert(0, $"Copied: {copied}{Environment.NewLine}Sent to Recycle Bin: {removed}{Environment.NewLine}{Environment.NewLine}");
        return sb.ToString();
    }

    public static string Describe(IReadOnlyList<SyncChange> plan)
    {
        var sb = new StringBuilder($"Changes: {plan.Count}{Environment.NewLine}{Environment.NewLine}");
        foreach (var change in plan) sb.AppendLine($"{change.Action.ToUpperInvariant()}  {change.Relative}");
        if (plan.Count == 0) sb.AppendLine("The destination already matches the source.");
        return sb.ToString();
    }

    public static void MergePdf(IReadOnlyList<string> files, string dest)
    {
        using var output = new PdfDocument();
        foreach (string file in files)
        {
            using var input = PdfReader.Open(file, PdfDocumentOpenMode.Import);
            for (int i = 0; i < input.PageCount; i++) output.AddPage(input.Pages[i]);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        output.Save(dest);
    }

    public static int SplitPdf(string file, string outDir)
    {
        using var input = PdfReader.Open(file, PdfDocumentOpenMode.Import);
        string stem = Path.GetFileNameWithoutExtension(file);
        for (int i = 0; i < input.PageCount; i++)
        {
            using var page = new PdfDocument();
            page.AddPage(input.Pages[i]);
            page.Save(UniquePath(outDir, $"{stem}_p{(i + 1):D3}.pdf"));
        }
        return input.PageCount;
    }

    public static (long Before, long After, string Dest) CompressPdf(string file, string outDir)
    {
        long before = new FileInfo(file).Length;
        string dest = UniquePath(outDir, Path.GetFileNameWithoutExtension(file) + "_compressed.pdf");
        using (var input = PdfReader.Open(file, PdfDocumentOpenMode.Import))
        using (var output = new PdfDocument())
        {
            for (int i = 0; i < input.PageCount; i++) output.AddPage(input.Pages[i]);
            output.Save(dest);
        }
        return (before, new FileInfo(dest).Length, dest);
    }

    public static string ListArchive(string archive)
    {
        using var opened = ArchiveFactory.OpenArchive(archive);
        var sb = new StringBuilder();
        foreach (var entry in opened.Entries)
            sb.AppendLine($"{(entry.IsDirectory ? "DIR " : "FILE")}  {entry.Key}  {(entry.IsDirectory ? "" : entry.Size.ToString("N0") + " bytes")}");
        return sb.Length == 0 ? "The archive is empty." : sb.ToString();
    }

    public static int ExtractArchive(string archive, string dest)
    {
        Directory.CreateDirectory(dest);
        int n = 0;
        using var opened = ArchiveFactory.OpenArchive(archive);
        foreach (var entry in opened.Entries)
        {
            if (entry.IsDirectory) continue;
            string full = SafeJoin(dest, (entry.Key ?? "").Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            if (File.Exists(full)) throw new IOException("Refusing to overwrite: " + full);
            using var source = entry.OpenEntryStream();
            using var target = File.Create(full);
            source.CopyTo(target);
            n++;
        }
        return n;
    }

    public static void CreateArchive(IReadOnlyList<string> files, string dest, bool sevenZip)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dest))!);
        var type = sevenZip ? ArchiveType.SevenZip : ArchiveType.Zip;
        var compression = sevenZip ? CompressionType.LZMA : CompressionType.Deflate;
        using var stream = File.Create(dest);
        using var writer = WriterFactory.OpenWriter(stream, type, new WriterOptions(compression));
        foreach (string file in files)
            using (var input = File.OpenRead(file))
                writer.Write(Path.GetFileName(file), input, null);
    }

    public static (long Before, long After, string Dest) CompressImage(string file, string outDir, bool webp, int quality)
    {
        if (quality is < 1 or > 100) throw new ArgumentException("Quality must be from 1 to 100.");
        using var bitmap = SKBitmap.Decode(file) ?? throw new IOException("This file is not a supported image.");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(webp ? SKEncodedImageFormat.Webp : SKEncodedImageFormat.Jpeg, quality);
        string ext = webp ? ".webp" : ".jpg";
        string dest = UniquePath(outDir, Path.GetFileNameWithoutExtension(file) + ext);
        using (var fs = File.Create(dest)) data.SaveTo(fs);
        return (new FileInfo(file).Length, new FileInfo(dest).Length, dest);
    }

    public static string Metadata(string file)
    {
        var info = new FileInfo(file);
        var sb = new StringBuilder();
        sb.AppendLine($"Name: {info.Name}");
        sb.AppendLine($"Path: {info.FullName}");
        sb.AppendLine($"Size: {info.Length:N0} bytes");
        sb.AppendLine($"Extension: {(string.IsNullOrEmpty(info.Extension) ? "(none)" : info.Extension)}");
        sb.AppendLine($"Created: {info.CreationTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Accessed: {info.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Attributes: {info.Attributes}");
        if (info.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var doc = PdfReader.Open(file, PdfDocumentOpenMode.Import);
                sb.AppendLine($"PDF pages: {doc.PageCount}");
            }
            catch (Exception ex) { sb.AppendLine($"PDF: {ex.Message}"); }
        }
        try
        {
            using var bitmap = SKBitmap.Decode(file);
            if (bitmap != null) sb.AppendLine($"Image: {bitmap.Width} x {bitmap.Height} px");
        }
        catch { /* not an image */ }
        return sb.ToString();
    }

    public static string UniquePath(string folder, string name)
    {
        string path = Path.Combine(folder, name);
        if (!File.Exists(path)) return path;
        string stem = Path.GetFileNameWithoutExtension(name), ext = Path.GetExtension(name);
        int n = 1;
        do { path = Path.Combine(folder, $"{stem}_{n++}{ext}"); } while (File.Exists(path));
        return path;
    }

    public static string SafeJoin(string root, string relative)
    {
        string fullRoot = Path.GetFullPath(root);
        string combined = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!IsInside(fullRoot, combined) && !combined.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Archive entry escapes the output folder: " + relative);
        return combined;
    }

    static bool IsInside(string root, string path)
    {
        string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    static Dictionary<string, string> FilesUnder(string root)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (new FileInfo(file).Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
            map[Path.GetRelativePath(root, file)] = file;
        }
        return map;
    }

    public static void SelfCheck()
    {
        string name = ExpandTemplate("{date}_{n}_{name}", @"C:\docs\Report.pdf", 2, new DateTime(2026, 11, 24));
        if (name != "20261124_002_Report.pdf") throw new InvalidOperationException("Template check failed: " + name);
        bool escaped = false;
        try { SafeJoin(@"C:\out", @"..\secret.txt"); }
        catch (IOException) { escaped = true; }
        if (!escaped) throw new InvalidOperationException("Path check failed.");

        string root = Path.Combine(Path.GetTempPath(), "lt-sync-" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(root, "src");
        string dest = Path.Combine(root, "dst");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(dest);
        try
        {
            string sample = Path.Combine(source, "a.txt");
            File.WriteAllText(sample, "hello");
            var plan = PlanSync(source, dest, true);
            if (plan.Count != 1 || plan[0].Action != "copy") throw new InvalidOperationException("Sync plan check failed.");

            string zip = Path.Combine(root, "a.zip");
            CreateArchive(new[] { sample }, zip, false);
            string extracted = Path.Combine(root, "out");
            if (ExtractArchive(zip, extracted) != 1 || File.ReadAllText(Path.Combine(extracted, "a.txt")) != "hello")
                throw new InvalidOperationException("Zip round-trip check failed.");

            string pdf = Path.Combine(root, "a.pdf");
            using (var doc = new PdfDocument()) { doc.AddPage(); doc.Save(pdf); }
            string merged = Path.Combine(root, "m.pdf");
            MergePdf(new[] { pdf, pdf }, merged);
            using (var check = PdfReader.Open(merged, PdfDocumentOpenMode.Import))
                if (check.PageCount != 2) throw new InvalidOperationException("PDF merge check failed.");

            using var bitmap = new SKBitmap(2, 2);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, 80);
            if (encoded == null || encoded.Size < 8) throw new InvalidOperationException("WebP encode check failed.");
        }
        finally { Directory.Delete(root, true); }
    }

    sealed record RenameStep(string From, string To);
}
