using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace LavistechFileToolkit;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, TextBox> _fields = new();
    private readonly Dictionary<string, ComboBox> _choices = new();
    private readonly List<string> _selectedFiles = new();
    private List<List<string>> _dupGroups = new();
    private string _tool = "Dashboard";

    public MainWindow()
    {
        FileOps.SelfCheck();
        InitializeComponent();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string name) return;
        if (name == "About") { OpenAbout(); return; }
        _tool = name;
        bool dash = name == "Dashboard";
        Dashboard.Visibility = dash ? Visibility.Visible : Visibility.Collapsed;
        ToolHost.Visibility = dash ? Visibility.Collapsed : Visibility.Visible;
        PageTitle.Text = dash ? "File Toolkit Dashboard" : name;
        PageSubtitle.Text = dash ? "A practical toolbox for managing, comparing and converting files" : "Local file operations • No cloud upload";
        if (!dash) BuildTool(name);
        foreach (var nav in NavPanel.Children.OfType<Button>())
        {
            bool active = Equals(nav.Tag, name);
            nav.Background = active ? System.Windows.Media.Brushes.DodgerBlue : System.Windows.Media.Brushes.Transparent;
            nav.Foreground = active ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Gainsboro;
        }
    }

    private void BuildTool(string name)
    {
        ToolControls.Children.Clear(); OutputBox.Clear(); StatusText.Text = ""; _fields.Clear(); _choices.Clear(); _selectedFiles.Clear(); _dupGroups = new();
        ToolDescription.Text = name switch
        {
            "Bulk Rename" => "Drop files here. Use a template such as {date}_{n}_{name}, or the prefix and suffix fields. Preview, apply, then undo the last apply.",
            "Duplicate Finder" => "Drop a folder here. Files are grouped when their size and SHA-256 match. Extra copies can be sent to the Recycle Bin. One file in each group is kept.",
            "File Hashing" => "Drop files here. Calculate MD5, SHA-1, SHA-256 or SHA-512 checksums.",
            "Folder Compare" => "Drop two folders here. Compare relative paths and sizes, or SHA-256 contents.",
            "Folder Sync" => "Copy new and newer files from the first folder into the second. Removing extras sends them to the Recycle Bin.",
            "Image Resizer" => "Drop images here. Resize to fixed dimensions or a percentage and save PNG copies.",
            "Format Converter" => "Drop images here. Convert between PNG, JPG, BMP, GIF and TIFF. Originals are kept.",
            "Image Compress" => "Drop images here. Save JPEG or WebP copies at the quality you choose and compare file sizes.",
            "PDF Tools" => "Drop PDFs here. Merge them, split one PDF into single pages, or rewrite a PDF and compare the size.",
            "Archives" => "Drop files to archive, or drop a ZIP or 7Z to list and extract. Extraction never writes outside the output folder.",
            "File Metadata" => "Drop a file here. Shows size, dates, attributes, and image or PDF details when available.",
            _ => ""
        };
        switch (name)
        {
            case "Bulk Rename":
                AddBrowseFiles("Select files to rename", false);
                AddInput("template", "Template (optional: {name} {ext} {n} {date} {parent})", 34);
                AddInput("prefix", "Prefix", 34);
                AddInput("suffix", "Suffix (inserted before extension)", 34);
                AddInput("find", "Find text in filename (optional)", 34);
                AddInput("replace", "Replace with", 34);
                AddChoice("sequence", "Numbering", new[] { "No numbering", "Prefix number (001, 002…)", "Suffix number (001, 002…)" });
                AddInput("start", "Starting number", 34, "1");
                AddAction("Preview rename", PreviewRename);
                AddAction("Apply rename", ApplyRename);
                AddAction("Undo last rename", () => { OutputBox.Text = FileOps.UndoLast(); StatusText.Text = "Undo finished."; });
                break;
            case "Duplicate Finder":
                AddFolderPicker("folder", "Folder to scan");
                AddAction("Find duplicates", FindDuplicates);
                AddAction("Recycle extra copies", RecycleDuplicates);
                break;
            case "File Hashing":
                AddBrowseFiles("Select files to hash", true);
                AddChoice("algorithm", "Hash algorithm", new[] { "SHA256", "SHA512", "SHA1", "MD5" });
                AddAction("Calculate hashes", HashFiles);
                break;
            case "Folder Compare":
                AddFolderPicker("folderA", "First folder");
                AddFolderPicker("folderB", "Second folder");
                AddChoice("deep", "Comparison mode", new[] { "Compare paths and file sizes", "Compare file contents (SHA-256)" });
                AddAction("Compare folders", CompareFolders);
                break;
            case "Folder Sync":
                AddFolderPicker("folderA", "Source folder");
                AddFolderPicker("folderB", "Destination folder");
                AddChoice("syncMode", "Sync mode", new[] { "Copy new and newer files", "Copy and recycle extras in the destination" });
                AddAction("Preview sync", PreviewSync);
                AddAction("Apply sync", ApplySync);
                break;
            case "File Metadata":
                AddBrowseFiles("Select files", true);
                AddAction("Show metadata", ShowMetadata);
                break;
            case "Image Resizer":
                AddBrowseFiles("Select images", true);
                AddFolderPicker("outdir", "Output folder");
                AddChoice("resizeMode", "Resize mode", new[] { "Set width and height", "Percentage" });
                AddInput("width", "Width in pixels", 34, "1280");
                AddInput("height", "Height in pixels", 34, "720");
                AddInput("percent", "Scale percentage", 34, "50");
                AddChoice("keep", "Aspect ratio", new[] { "Fit inside dimensions (keep ratio)", "Stretch to exact dimensions" });
                AddAction("Resize images", ResizeImages);
                break;
            case "Format Converter":
                AddBrowseFiles("Select images to convert", true);
                AddFolderPicker("outdir", "Output folder");
                AddChoice("format", "Output format", new[] { "PNG", "JPG", "BMP", "GIF", "TIFF" });
                AddAction("Convert images", ConvertImages);
                break;
            case "Image Compress":
                AddBrowseFiles("Select images", true);
                AddFolderPicker("outdir", "Output folder");
                AddChoice("imgfmt", "Output", new[] { "JPEG", "WebP" });
                AddInput("quality", "Quality (1–100)", 34, "75");
                AddAction("Compress images", CompressImages);
                break;
            case "PDF Tools":
                AddBrowseFiles("Select PDF files", true);
                AddFolderPicker("outdir", "Output folder");
                AddChoice("pdfop", "Operation", new[] { "Merge into one PDF", "Split into single pages", "Recompress" });
                AddInput("pdfname", "Merged file name", 34, "merged.pdf");
                AddAction("Run PDF tool", RunPdf);
                break;
            case "Archives":
                AddBrowseFiles("Files to put in a new archive", true);
                AddArchivePicker();
                AddFolderPicker("outdir", "Extract or archive output folder");
                AddChoice("arctype", "New archive type", new[] { "ZIP", "7Z" });
                AddAction("List archive", ListArchive);
                AddAction("Extract archive", ExtractArchive);
                AddAction("Create archive", CreateArchive);
                break;
        }
    }

    private void OpenAbout()
    {
        var existing = Application.Current.Windows.OfType<AboutWindow>().FirstOrDefault();
        if (existing != null) { existing.Activate(); return; }
        new AboutWindow { Owner = this }.Show();
    }

    private void AddLabel(string text) => ToolControls.Children.Add(new TextBlock { Text = text, Foreground = System.Windows.Media.Brushes.LightGray, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 9, 0, 5) });
    private void AddInput(string key, string label, double height, string value = "")
    {
        AddLabel(label);
        var t = new TextBox { Text = value, Height = height, AcceptsReturn = height > 50, TextWrapping = TextWrapping.Wrap };
        _fields[key] = t; ToolControls.Children.Add(t);
    }
    private void AddChoice(string key, string label, string[] options)
    {
        AddLabel(label);
        var c = new ComboBox { ItemsSource = options, SelectedIndex = 0, Height = 34, HorizontalAlignment = HorizontalAlignment.Left };
        _choices[key] = c; ToolControls.Children.Add(c);
    }
    private void AddFolderPicker(string key, string label)
    {
        AddLabel(label);
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var t = new TextBox { Width = 600, Height = 36, IsReadOnly = true, AcceptsReturn = false };
        var btn = new Button { Content = "Browse…", Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(8,0,0,0) };
        _fields[key] = t;
        btn.Click += (_, _) => { using var d = new Forms.FolderBrowserDialog(); if (d.ShowDialog() == Forms.DialogResult.OK) t.Text = d.SelectedPath; };
        row.Children.Add(t); row.Children.Add(btn); ToolControls.Children.Add(row);
    }
    private void AddBrowseFiles(string label, bool multiple)
    {
        AddLabel(label);
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var t = new TextBox { Width = 600, Height = 36, IsReadOnly = true, AcceptsReturn = false };
        var btn = new Button { Content = "Choose files…", Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(8,0,0,0) };
        _fields["filesDisplay"] = t;
        btn.Click += (_, _) =>
        {
            var d = new OpenFileDialog { Multiselect = multiple, Title = label, Filter = "All files|*.*" };
            if (d.ShowDialog() == true) { _selectedFiles.Clear(); _selectedFiles.AddRange(d.FileNames); t.Text = $"{_selectedFiles.Count} file(s) selected"; }
        };
        row.Children.Add(t); row.Children.Add(btn); ToolControls.Children.Add(row);
    }
    private void AddArchivePicker()
    {
        AddLabel("Existing archive to list or extract");
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var t = new TextBox { Width = 600, Height = 36, IsReadOnly = true, AcceptsReturn = false };
        var btn = new Button { Content = "Choose archive…", Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(8, 0, 0, 0) };
        _fields["archive"] = t;
        btn.Click += (_, _) =>
        {
            var d = new OpenFileDialog { Filter = "Archives|*.zip;*.7z;*.rar;*.tar;*.gz|All files|*.*" };
            if (d.ShowDialog() == true) t.Text = d.FileName;
        };
        row.Children.Add(t); row.Children.Add(btn); ToolControls.Children.Add(row);
    }
    private void AddAction(string text, Action action)
    {
        var b = new Button { Content = text, Background = System.Windows.Media.Brushes.DodgerBlue, Foreground = System.Windows.Media.Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,14,8,0), MinWidth = 145 };
        b.Click += (_, _) => Run(action); ToolControls.Children.Add(b);
    }
    private string Val(string key) => _fields.TryGetValue(key, out var t) ? t.Text.Trim() : "";
    private int Choice(string key) => _choices[key].SelectedIndex;
    private void Run(Action action)
    {
        try { action(); if (string.IsNullOrWhiteSpace(StatusText.Text)) StatusText.Text = "Completed."; }
        catch (Exception ex) { StatusText.Text = "Error: " + ex.Message; }
    }

    private List<(string Old, string New)> MakeRenamePlan()
    {
        if (_selectedFiles.Count == 0) throw new InvalidOperationException("Select one or more files first.");
        int.TryParse(Val("start"), out int start); if (start < 0) start = 1;
        var result = new List<(string,string)>();
        for (int i=0;i<_selectedFiles.Count;i++)
        {
            string old = _selectedFiles[i], dir = Path.GetDirectoryName(old)!;
            string fileName;
            if (!string.IsNullOrWhiteSpace(Val("template"))) fileName = FileOps.ExpandTemplate(Val("template"), old, start + i, DateTime.Now);
            else
            {
                string ext = Path.GetExtension(old);
                string stem = Path.GetFileNameWithoutExtension(old);
                if (!string.IsNullOrEmpty(Val("find"))) stem = stem.Replace(Val("find"), Val("replace"), StringComparison.OrdinalIgnoreCase);
                stem = Val("prefix") + stem + Val("suffix");
                string num = (start + i).ToString("D3");
                if (Choice("sequence") == 1) stem = num + "_" + stem;
                if (Choice("sequence") == 2) stem += "_" + num;
                fileName = stem + ext;
            }
            result.Add((old, Path.Combine(dir, fileName)));
        }
        return result;
    }
    private void PreviewRename()
    {
        var plan=MakeRenamePlan(); var sb=new StringBuilder("RENAME PREVIEW\n");
        foreach(var p in plan) sb.AppendLine($"{Path.GetFileName(p.Old)}  →  {Path.GetFileName(p.New)}");
        OutputBox.Text=sb.ToString(); StatusText.Text="Preview only. Select Apply rename to perform changes.";
    }
    private void ApplyRename()
    {
        var plan=MakeRenamePlan();
        var collisions=plan.GroupBy(x=>x.New,StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).Select(g=>g.Key).ToList();
        if(collisions.Count>0) throw new IOException("Multiple files would have the same destination name.");
        foreach(var p in plan) if(!string.Equals(p.Old,p.New,StringComparison.OrdinalIgnoreCase) && File.Exists(p.New)) throw new IOException($"Destination already exists: {p.New}");
        int done=0;
        foreach(var p in plan) { if(!string.Equals(p.Old,p.New,StringComparison.OrdinalIgnoreCase)) { File.Move(p.Old,p.New); done++; } }
        FileOps.SaveUndo(plan);
        OutputBox.Text=$"Rename operation finished.\nFiles renamed: {done}\n\n"+string.Join(Environment.NewLine,plan.Select(p=>$"{Path.GetFileName(p.Old)} → {Path.GetFileName(p.New)}"));
        StatusText.Text="Rename completed. Original file contents were not modified.";
        // Update selection so a subsequent preview reflects current names.
        _selectedFiles.Clear(); _selectedFiles.AddRange(plan.Select(p=>p.New)); _fields["filesDisplay"].Text=$"{_selectedFiles.Count} file(s) selected";
    }

    private static IEnumerable<string> EnumerateFiles(string folder) => Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories);
    private static string HashFile(string file, string algo="SHA256")
    {
        using var s=File.OpenRead(file);
        byte[] h=algo switch { "MD5"=>MD5.HashData(s), "SHA1"=>SHA1.HashData(s), "SHA512"=>SHA512.HashData(s), _=>SHA256.HashData(s) };
        return Convert.ToHexString(h).ToLowerInvariant();
    }
    private void FindDuplicates()
    {
        string folder=Val("folder"); if(!Directory.Exists(folder)) throw new DirectoryNotFoundException("Choose an existing folder.");
        var files=EnumerateFiles(folder).ToList(); var groups=new Dictionary<string,List<string>>();
        int i=0;
        foreach(var f in files) { try { string key=new FileInfo(f).Length+":"+HashFile(f); if(!groups.ContainsKey(key))groups[key]=new(); groups[key].Add(f); } catch{} i++; }
        var dup=groups.Where(g=>g.Value.Count>1).ToList();
        _dupGroups = dup.Select(g => g.Value).ToList();
        var sb=new StringBuilder($"Scanned: {files.Count} files\nDuplicate groups: {dup.Count}\n\n");
        foreach(var g in dup) { sb.AppendLine($"GROUP ({new FileInfo(g.Value[0]).Length:N0} bytes)"); foreach(var f in g.Value) sb.AppendLine("  "+f); sb.AppendLine(); }
        OutputBox.Text=sb.ToString(); StatusText.Text="Scan complete. No files were deleted or changed.";
    }
    private void HashFiles()
    {
        if(_selectedFiles.Count==0)throw new InvalidOperationException("Select files first.");
        string algo=new[]{"SHA256","SHA512","SHA1","MD5"}[Choice("algorithm")];
        var sb=new StringBuilder($"Algorithm: {algo}\n");
        foreach(var f in _selectedFiles) sb.AppendLine($"{HashFile(f,algo)}  {f}");
        OutputBox.Text=sb.ToString(); StatusText.Text=$"Hashed {_selectedFiles.Count} file(s).";
    }
    private void CompareFolders()
    {
        string a=Val("folderA"),b=Val("folderB");
        if(!Directory.Exists(a)||!Directory.Exists(b))throw new DirectoryNotFoundException("Choose both folders.");
        var left=EnumerateFiles(a).ToDictionary(f=>Path.GetRelativePath(a,f),f=>f,StringComparer.OrdinalIgnoreCase);
        var right=EnumerateFiles(b).ToDictionary(f=>Path.GetRelativePath(b,f),f=>f,StringComparer.OrdinalIgnoreCase);
        var keys=left.Keys.Union(right.Keys,StringComparer.OrdinalIgnoreCase).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase);
        var sb=new StringBuilder("FOLDER COMPARISON\n");
        int same=0,diff=0,onlyA=0,onlyB=0;
        foreach(var k in keys)
        {
            if(!left.ContainsKey(k)){sb.AppendLine($"ONLY IN SECOND: {k}");onlyB++;continue;}
            if(!right.ContainsKey(k)){sb.AppendLine($"ONLY IN FIRST:  {k}");onlyA++;continue;}
            var x=new FileInfo(left[k]);var y=new FileInfo(right[k]);
            bool equal=x.Length==y.Length;
            if(equal && Choice("deep")==1) equal=HashFile(left[k])==HashFile(right[k]);
            if(equal){same++;}else{diff++;sb.AppendLine($"DIFFERENT: {k} (first {x.Length:N0} bytes; second {y.Length:N0} bytes)");}
        }
        OutputBox.Text=$"First folder: {a}\nSecond folder: {b}\nSame: {same}\nDifferent: {diff}\nOnly in first: {onlyA}\nOnly in second: {onlyB}\n\n{sb}";
        StatusText.Text="Comparison complete. Files were not modified.";
    }

    private BitmapSource LoadBitmap(string file)
    {
        using var fs=File.OpenRead(file);
        var decoder=BitmapDecoder.Create(fs,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }
    private static BitmapEncoder Encoder(string fmt) => fmt switch
    {
        "JPG"=>new JpegBitmapEncoder { QualityLevel=92 },
        "BMP"=>new BmpBitmapEncoder(),
        "GIF"=>new GifBitmapEncoder(),
        "TIFF"=>new TiffBitmapEncoder(),
        _=>new PngBitmapEncoder()
    };
    private void ResizeImages()
    {
        if(_selectedFiles.Count==0)throw new InvalidOperationException("Select images first.");
        string output=Val("outdir"); if(!Directory.Exists(output))throw new DirectoryNotFoundException("Choose an existing output folder.");
        int w=0,h=0,pct=0; int.TryParse(Val("width"),out w);int.TryParse(Val("height"),out h);int.TryParse(Val("percent"),out pct);
        var sb=new StringBuilder();int ok=0;
        foreach(var f in _selectedFiles)
        {
            try
            {
                var src=LoadBitmap(f); double nw,nh;
                if(Choice("resizeMode")==1){if(pct<1||pct>500)throw new ArgumentException("Percentage must be between 1 and 500.");nw=src.PixelWidth*pct/100.0;nh=src.PixelHeight*pct/100.0;}
                else {if(w<1||h<1)throw new ArgumentException("Width and height must be positive.");if(Choice("keep")==0){double scale=Math.Min((double)w/src.PixelWidth,(double)h/src.PixelHeight);nw=src.PixelWidth*scale;nh=src.PixelHeight*scale;}else{nw=w;nh=h;}}
                var scaled=new TransformedBitmap(src,new System.Windows.Media.ScaleTransform(nw/src.PixelWidth,nh/src.PixelHeight));
                string dest=FileOps.UniquePath(output,Path.GetFileNameWithoutExtension(f)+"_resized.png");
                SaveBitmap(scaled,dest,"PNG");sb.AppendLine($"{f} → {dest} ({(int)nw}x{(int)nh})");ok++;
            }catch(Exception ex){sb.AppendLine($"FAILED {f}: {ex.Message}");}
        }
        OutputBox.Text=sb.ToString();StatusText.Text=$"Resized {ok} of {_selectedFiles.Count} image(s).";
    }
    private void ConvertImages()
    {
        if(_selectedFiles.Count==0)throw new InvalidOperationException("Select images first.");
        string output=Val("outdir");if(!Directory.Exists(output))throw new DirectoryNotFoundException("Choose an existing output folder.");
        string fmt=new[]{"PNG","JPG","BMP","GIF","TIFF"}[Choice("format")];
        string ext=fmt.ToLowerInvariant();if(fmt=="JPG")ext="jpg";
        var sb=new StringBuilder();int ok=0;
        foreach(var f in _selectedFiles)
        {
            try{var bmp=LoadBitmap(f);string dest=FileOps.UniquePath(output,Path.GetFileNameWithoutExtension(f)+"."+ext);SaveBitmap(bmp,dest,fmt);sb.AppendLine($"{f} → {dest}");ok++;}
            catch(Exception ex){sb.AppendLine($"FAILED {f}: {ex.Message}");}
        }
        OutputBox.Text=sb.ToString();StatusText.Text=$"Converted {ok} of {_selectedFiles.Count} image(s).";
    }
    private static void SaveBitmap(BitmapSource bitmap,string path,string format)
    {
        var enc=Encoder(format);enc.Frames.Add(BitmapFrame.Create(bitmap));
        using var fs=File.Create(path);enc.Save(fs);
    }
    private void RecycleDuplicates()
    {
        if (_dupGroups.Count == 0) throw new InvalidOperationException("Find duplicates first.");
        var extras = new List<string>();
        foreach (var group in _dupGroups)
        {
            var keep = group.OrderBy(f => new FileInfo(f).CreationTimeUtc).ThenBy(f => f, StringComparer.OrdinalIgnoreCase).First();
            extras.AddRange(group.Where(f => !string.Equals(f, keep, StringComparison.OrdinalIgnoreCase)));
        }
        if (extras.Count == 0) { StatusText.Text = "No extra copies to remove."; return; }
        if (MessageBox.Show(this, $"Move {extras.Count} extra file(s) to the Recycle Bin? The oldest file in each group is kept.", "Lavistech File Toolkit", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var sb = new StringBuilder();
        int done = 0;
        foreach (string file in extras)
        {
            try { FileOps.Recycle(file); sb.AppendLine("Recycled " + file); done++; }
            catch (Exception ex) { sb.AppendLine($"FAILED {file}: {ex.Message}"); }
        }
        OutputBox.Text = sb.ToString();
        StatusText.Text = $"Sent {done} of {extras.Count} file(s) to the Recycle Bin.";
        _dupGroups = new();
    }

    private void PreviewSync()
    {
        var plan = FileOps.PlanSync(RequireDir("folderA"), RequireDir("folderB"), Choice("syncMode") == 1);
        OutputBox.Text = FileOps.Describe(plan);
        StatusText.Text = "Preview only. Apply sync to change the destination.";
    }

    private void ApplySync()
    {
        bool remove = Choice("syncMode") == 1;
        var plan = FileOps.PlanSync(RequireDir("folderA"), RequireDir("folderB"), remove);
        int deletions = plan.Count(p => p.Action == "delete");
        if (deletions > 0 && MessageBox.Show(this, $"Send {deletions} file(s) that are only in the destination to the Recycle Bin?", "Lavistech File Toolkit", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        OutputBox.Text = FileOps.ApplySync(plan);
        StatusText.Text = "Sync finished.";
    }

    private void ShowMetadata()
    {
        if (_selectedFiles.Count == 0) throw new InvalidOperationException("Select a file first.");
        OutputBox.Text = string.Join(Environment.NewLine + Environment.NewLine, _selectedFiles.Select(FileOps.Metadata));
    }

    private void CompressImages()
    {
        if (_selectedFiles.Count == 0) throw new InvalidOperationException("Select images first.");
        string output = RequireDir("outdir");
        if (!int.TryParse(Val("quality"), out int quality)) throw new ArgumentException("Enter a quality from 1 to 100.");
        bool webp = Choice("imgfmt") == 1;
        var sb = new StringBuilder();
        int ok = 0;
        foreach (string file in _selectedFiles)
        {
            try
            {
                var result = FileOps.CompressImage(file, output, webp, quality);
                sb.AppendLine($"{file} → {result.Dest} ({result.Before:N0} → {result.After:N0} bytes)");
                ok++;
            }
            catch (Exception ex) { sb.AppendLine($"FAILED {file}: {ex.Message}"); }
        }
        OutputBox.Text = sb.ToString();
        StatusText.Text = $"Compressed {ok} of {_selectedFiles.Count} image(s). Originals were kept.";
    }

    private void RunPdf()
    {
        if (_selectedFiles.Count == 0) throw new InvalidOperationException("Select PDF files first.");
        string output = RequireDir("outdir");
        int mode = Choice("pdfop");
        if (mode == 0)
        {
            string name = Val("pdfname");
            if (string.IsNullOrWhiteSpace(name)) name = "merged.pdf";
            if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) name += ".pdf";
            string dest = FileOps.UniquePath(output, name);
            FileOps.MergePdf(_selectedFiles, dest);
            OutputBox.Text = $"Merged {_selectedFiles.Count} file(s) into {dest}";
        }
        else if (mode == 1)
        {
            var sb = new StringBuilder();
            foreach (string file in _selectedFiles) sb.AppendLine($"{file}: {FileOps.SplitPdf(file, output)} page(s)");
            OutputBox.Text = sb.ToString();
        }
        else
        {
            var sb = new StringBuilder();
            foreach (string file in _selectedFiles)
            {
                var result = FileOps.CompressPdf(file, output);
                sb.AppendLine($"{file} → {result.Dest} ({result.Before:N0} → {result.After:N0} bytes)");
            }
            OutputBox.Text = sb.ToString();
        }
        StatusText.Text = "PDF tool finished. This recompresses PDF streams. It does not downsample images inside the PDF.";
    }

    private void ListArchive() => OutputBox.Text = FileOps.ListArchive(RequireFile("archive"));

    private void ExtractArchive()
    {
        int n = FileOps.ExtractArchive(RequireFile("archive"), RequireDir("outdir"));
        OutputBox.Text = $"Extracted {n} file(s). Existing files were not overwritten.";
        StatusText.Text = "Extract finished.";
    }

    private void CreateArchive()
    {
        if (_selectedFiles.Count == 0) throw new InvalidOperationException("Select files to archive.");
        string output = RequireDir("outdir");
        bool seven = Choice("arctype") == 1;
        string dest = FileOps.UniquePath(output, seven ? "archive.7z" : "archive.zip");
        FileOps.CreateArchive(_selectedFiles, dest, seven);
        OutputBox.Text = $"Created {dest} with {_selectedFiles.Count} file(s).";
    }

    private string RequireDir(string key)
    {
        string path = Val(key);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Choose an existing folder.");
        return path;
    }

    private string RequireFile(string key)
    {
        string path = Val(key);
        if (!File.Exists(path)) throw new FileNotFoundException("Choose an existing file.");
        return path;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = ((string[])e.Data.GetData(DataFormats.FileDrop)!).ToList();
        var files = paths.Where(File.Exists).ToList();
        var dirs = paths.Where(Directory.Exists).ToList();
        if (_tool is "Dashboard" or "About") { PageSubtitle.Text = "Open a tool, then drop files or folders here."; return; }
        if (_tool is "Duplicate Finder")
        {
            if (dirs.Count == 0) { StatusText.Text = "Drop a folder to scan."; return; }
            SetField("folder", dirs[0]);
            StatusText.Text = "Folder set. Choose Find duplicates.";
            return;
        }
        if (_tool is "Folder Compare" or "Folder Sync")
        {
            foreach (string dir in dirs)
            {
                if (string.IsNullOrWhiteSpace(Val("folderA"))) SetField("folderA", dir);
                else if (string.IsNullOrWhiteSpace(Val("folderB"))) SetField("folderB", dir);
            }
            StatusText.Text = dirs.Count == 0 ? "Drop folders." : "Folder set.";
            return;
        }
        if (_tool == "Archives")
        {
            var archive = files.FirstOrDefault(f => IsArchive(f));
            if (archive != null && _fields.ContainsKey("archive")) SetField("archive", archive);
            var rest = files.Where(f => !IsArchive(f)).ToList();
            foreach (string dir in dirs) rest.AddRange(Directory.GetFiles(dir));
            if (rest.Count > 0) SetFiles(rest);
            StatusText.Text = "Drop received.";
            return;
        }
        if (_tool is "Image Resizer" or "Format Converter" or "Image Compress")
        {
            foreach (string dir in dirs) files.AddRange(Directory.GetFiles(dir));
            SetFiles(files);
            if (dirs.Count > 0 && _fields.ContainsKey("outdir") && string.IsNullOrWhiteSpace(Val("outdir"))) SetField("outdir", dirs[0]);
            StatusText.Text = $"{_selectedFiles.Count} file(s) ready.";
            return;
        }
        foreach (string dir in dirs) files.AddRange(Directory.GetFiles(dir));
        if (_tool == "PDF Tools") files = files.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();
        SetFiles(files);
        StatusText.Text = $"{_selectedFiles.Count} file(s) ready.";
    }

    private static bool IsArchive(string path)
    {
        string ext = Path.GetExtension(path);
        return ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) || ext.Equals(".7z", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".rar", StringComparison.OrdinalIgnoreCase) || ext.Equals(".tar", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".gz", StringComparison.OrdinalIgnoreCase);
    }

    private void SetFiles(List<string> files)
    {
        _selectedFiles.Clear();
        _selectedFiles.AddRange(files.Distinct(StringComparer.OrdinalIgnoreCase));
        if (_fields.TryGetValue("filesDisplay", out var box)) box.Text = $"{_selectedFiles.Count} file(s) selected";
    }

    private void SetField(string key, string value)
    {
        if (_fields.TryGetValue(key, out var box)) box.Text = value;
    }

    private void Copy_Click(object sender,RoutedEventArgs e){if(!string.IsNullOrEmpty(OutputBox.Text)){Clipboard.SetText(OutputBox.Text);StatusText.Text="Results copied.";}}
    private void Clear_Click(object sender,RoutedEventArgs e){OutputBox.Clear();StatusText.Text="";}
    private void SearchBox_TextChanged(object sender,TextChangedEventArgs e)
    {
        if(NavPanel is null||SearchBox is null)return;
        string q=SearchBox.Text.Trim();
        foreach(var b in NavPanel.Children.OfType<Button>()) b.Visibility=string.IsNullOrEmpty(q)||(b.Tag?.ToString()??"").Contains(q,StringComparison.OrdinalIgnoreCase)?Visibility.Visible:Visibility.Collapsed;
    }
}