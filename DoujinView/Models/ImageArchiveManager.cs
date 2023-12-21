using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DoujinView.Models;

public static class ImageArchiveManager {
    public static Color  MainColor           { get; private set; } = Colors.Black;
    public static Color  HeaderColor         { get; private set; } = Colors.Black;
    public static string ArchiveSize         { get; private set; } = string.Empty;
    public static string NextArchiveName     { get; private set; } = "No Data";
    public static string PreviousArchiveName { get; private set; } = "No Data";
    public static int   CurrentPageWidth    => ImageBuffer[0]?.PixelSize.Width ?? 0;
    public static int   CurrentPageHeight   => ImageBuffer[0]?.PixelSize.Height ?? 0;

    public static Bitmap?[] ImageBuffer       { get; } = new Bitmap?[2];
    public static string    ArchiveName       => Path.GetFileNameWithoutExtension(App.PathArg);
    public static string    ArchivePath       => App.PathArg;
    public static string    ArchiveType       => Path.GetExtension(App.PathArg).ToUpperInvariant().TrimStart('.');
    public static int       ArchivePages      => _archive.Length;
    public static string    CurrentPageSize   => _archive[CurrentPageIndex]?.CompressedLength.ToFileSize() ?? string.Empty;
    public static string    CurrentPageName   => _archive[CurrentPageIndex]?.Name ?? string.Empty;
    public static int       CurrentPageNumber => CurrentPageIndex + 1;
    public static int       TotalPages        => _archive.Length;

    static readonly Dictionary<int, Color> _backgroundColors = new();
    static          ZipArchiveEntry?[]     _archive          = Array.Empty<ZipArchiveEntry>();
    static          int                    _currentPageIndex = 0;

    public static event Action? OnCurrentPageProcessed;
    public static event Action? OnNextPageProcessed;

    public static int CurrentPageIndex {
        get => _currentPageIndex;
        set => _currentPageIndex = _archive.Length == 0 ? 0 : Math.Clamp(value, 0, _archive.Length - 1);
    }

    public static event Action? OnArchiveOpened;

    public static async Task Initialize(string path) {
        if (string.IsNullOrEmpty(path) || !Path.Exists(path)) return;
        _backgroundColors.Clear();
        App.PathArg = path;
        Console.WriteLine($"Loading {path}...");
        try {
            _archive = ZipFile.Open(path, ZipArchiveMode.Read).Entries
                              .Where(entry => entry.IsImage())
                              .OrderBy(entry => entry.Name.Length)
                              .ThenBy(entry => entry.Name)
                              .ToArray();
        }
        catch (Exception e) {
            Console.WriteLine("Failed to load archive: " + path);
            return;
        }

        ClearImageBuffer();
        CurrentPageIndex = 0;
        ArchiveSize = new FileInfo(path).Length.ToFileSize();
        App.SaveToAppConfiguration("LastOpenedFilePath", path);
        OnArchiveOpened?.Invoke();
        await UpdateImageBufferToNext();
    }

    public static void OpenArchiveFolderAndFocus() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{App.PathArg}\"");
    }

    public static async Task OpenNextFile() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var files = GetArchives();
        var index = Array.IndexOf(files, App.PathArg);
        if (index < files.Length - 1) {
            PreviousArchiveName = Path.GetFileNameWithoutExtension(App.PathArg);
            App.PathArg = files[index + 1];
            NextArchiveName = index < files.Length - 2 ? Path.GetFileNameWithoutExtension(files[index + 2]) : string.Empty;
            await Initialize(App.PathArg);
        }
    }

    public static async Task OpenPreviousFile() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var files = GetArchives();
        var index = Array.IndexOf(files, App.PathArg);
        if (index > 0) {
            NextArchiveName = index > 1 ? Path.GetFileNameWithoutExtension(App.PathArg) : string.Empty;
            App.PathArg = files[index - 1];
            PreviousArchiveName =  index < files.Length - 2 ? Path.GetFileNameWithoutExtension(files[index - 2]) : string.Empty;
            await Initialize(App.PathArg);
        }
    }

    static string[] GetArchives() =>
        Directory.GetFiles(Path.GetDirectoryName(App.PathArg) ?? string.Empty)
                 .Where(FileIsValidArchive)
                  // .OrderBy(File.GetCreationTime)
                 .ToArray();

    static void ClearImageBuffer() {
        ImageBuffer[0]?.Dispose();
        ImageBuffer[1]?.Dispose();
        ImageBuffer[0] = null;
        ImageBuffer[1] = null;
    }

    static bool FileIsValidArchive(string file) => file.EndsWith(".zip") || file.EndsWith(".rar") || file.EndsWith(".cbr") || file.EndsWith(".cbz");

    public static async Task UpdateImageBufferToNext(bool refreshAll = false) {
        ImageBuffer[0] = ImageBuffer[1] is null || refreshAll
                             ? await _archive[CurrentPageIndex]?.GetBitmapAsync()!
                             : ImageBuffer[1];
        await UpdateBackgroundColor(CurrentPageIndex);
        OnCurrentPageProcessed?.Invoke();

        ImageBuffer[1] = null;
        OnNextPageProcessed?.Invoke();

        ImageBuffer[1] = CurrentPageIndex < _archive.Length - 1
                             ? await _archive[CurrentPageIndex + 1]?.GetBitmapAsync()!
                             : null;
        OnNextPageProcessed?.Invoke();

        Console.WriteLine(ImageBuffer[0] is not null
                              ? $"Loaded {_archive[CurrentPageIndex + 1]?.Name} to main buffer"
                              : "No more images to load");
    }

    static async Task UpdateBackgroundColor(int pageIndex) {
        if (_backgroundColors.TryGetValue(pageIndex, out var color)) {
            MainColor = color;
            HeaderColor = Color.FromRgb((byte)(255 - MainColor.R), (byte)(255 - MainColor.G), (byte)(255 - MainColor.B));
            Console.WriteLine($"Loaded {CurrentPageName} color from cache: {color}");
            return;
        }

        MainColor = await _archive[pageIndex]?.ReadBackgroundPixel(10, 10)!;
        HeaderColor = Color.FromRgb((byte)(255 - MainColor.R), (byte)(255 - MainColor.G), (byte)(255 - MainColor.B));
        _backgroundColors.Add(pageIndex, MainColor);
        Console.WriteLine($"Saved {CurrentPageName} color to cache: {MainColor}");
    }

    public static async Task UpdateImageBufferToPrevious(bool refreshAll = false) {
        ImageBuffer[1] = CurrentPageIndex < _archive.Length - 1 && ImageBuffer[0] is null || refreshAll
                             ? await _archive[CurrentPageIndex + 1]?.GetBitmapAsync()!
                             : ImageBuffer[0];
        OnNextPageProcessed?.Invoke();

        ImageBuffer[0] = null;
        OnCurrentPageProcessed?.Invoke();

        ImageBuffer[0] = await _archive[CurrentPageIndex]?.GetBitmapAsync()!;
        await UpdateBackgroundColor(CurrentPageIndex);
        OnCurrentPageProcessed?.Invoke();

        Console.WriteLine(ImageBuffer[0] is not null
                              ? $"Loaded {_archive[CurrentPageIndex]?.Name} to main buffer"
                              : "No more images to load");
    }
}