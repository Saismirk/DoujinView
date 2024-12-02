using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DoujinView.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace DoujinView.Models;

public static class ImageArchiveManager {
    public static Color  MainColor           { get; private set; } = Colors.Black;
    public static Color  HeaderColor         { get; private set; } = Colors.Black;
    public static string ArchiveSize         { get; private set; } = string.Empty;
    public static string NextArchiveName     { get; private set; } = "No Data";
    public static string PreviousArchiveName { get; private set; } = "No Data";
    public static int    CurrentPageWidth    => ImageBuffer[0]?.PixelSize.Width ?? 0;
    public static int    CurrentPageHeight   => ImageBuffer[0]?.PixelSize.Height ?? 0;

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
        DebugLogger.Log($"Loading {path}...");
        try {
            _archive = ZipFile.Open(path, ZipArchiveMode.Read).Entries
                              .Where(entry => entry.IsImage())
                              .AsParallel()
                              .OrderBy(entry => entry.Name.Length)
                              .ThenBy(entry => entry.Name)
                              .ToArray();
            DebugLogger.Log($"Loaded {_archive.Length} files from {path}");
        }
        catch (Exception e) {
            DebugLogger.Log($"Failed to load archive: {path}\n{e}");
            return;
        }

        ClearImageBuffer();
        GC.Collect();
        CurrentPageIndex = 0;
        ArchiveSize = new FileInfo(path).Length.ToFileSize();
        App.SaveToAppConfiguration("LastOpenedFilePath", path);
        OnArchiveOpened?.Invoke();
        await UpdateImageBufferToNext(App.WindowHeight);
    }

    public static void OpenArchiveFolderAndFocus() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        try {
            DebugLogger.Log($"Opening archive folder ({App.PathArg})...");
            System.Diagnostics.Process.Start("explorer.exe", $"/select,{App.PathArg}");
        }
        catch {
            DebugLogger.Log($"Failed to open archive folder ({App.PathArg})");
        }
    }

    public static async Task DeleteArchiveFolder() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var fileToDeletePath = App.PathArg;
        var confirmationBox = MessageBoxManager.GetMessageBoxStandard("Delete File",
                                                                      $"Are you sure you want to delete the file ({fileToDeletePath})?",
                                                                      ButtonEnum.YesNo);
        if (await confirmationBox.ShowWindowAsync() is ButtonResult.No) return;
        DebugLogger.Log("Opening next file.before deleting...");
        await OpenNextFile();
        await Task.Delay(100);
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg) || fileToDeletePath == App.PathArg) return;
        try {
            DebugLogger.Log($"Deleting archive file ({fileToDeletePath})...", true);
            File.Delete(fileToDeletePath);
        }
        catch (Exception e) {
            DebugLogger.Log($"Failed to delete archive file ({App.PathArg}:\n{e.Message}", true);
        }
    }

    public static async Task OpenNextFile() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var files             = GetArchives();
        var time              = DateTime.UtcNow;
        var currentMatchFound = false;
        var nextMatchFound    = false;
        foreach (var file in files) {
            if (currentMatchFound && !nextMatchFound) {
                App.PathArg = file;
                Console.WriteLine($"Will open: {file}");
                nextMatchFound = true;
                continue;
            }

            if (nextMatchFound) {
                NextArchiveName = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine($"Found next file in: {(DateTime.UtcNow - time).TotalMilliseconds}ms");
                await Initialize(App.PathArg);
                break;
            }

            if (file == App.PathArg) {
                currentMatchFound = true;
                PreviousArchiveName = Path.GetFileNameWithoutExtension(file);
            }
        }

        GC.Collect();
    }

    public static async Task OpenPreviousFile() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var files                = GetArchives();
        var time                 = DateTime.UtcNow;
        var previousFile         = string.Empty;
        var previousPreviousFile = string.Empty;
        foreach (var file in files) {
            if (file == App.PathArg) {
                NextArchiveName = Path.GetFileNameWithoutExtension(App.PathArg);
                PreviousArchiveName = Path.GetFileNameWithoutExtension(previousPreviousFile);
                App.PathArg = previousFile;
                Console.WriteLine($"Found next file in: {(DateTime.UtcNow - time).TotalMilliseconds}ms");
                await Initialize(App.PathArg);
                break;
            }

            previousPreviousFile = previousFile;
            previousFile = file;
        }

        GC.Collect();
    }

    static ParallelQuery<string> GetArchives() {
        var time = DateTime.UtcNow;
        var archives = Directory.GetFiles(Path.GetDirectoryName(App.PathArg) ?? string.Empty)
                                .AsParallel()
                                .Where(FileIsValidArchive);
        GC.Collect();
        archives = Settings.CreationOrder.Value
                       ? archives.OrderBy(File.GetCreationTime)
                       : archives.OrderBy(file => file);
        DebugLogger.Log($"Found archives in {(DateTime.UtcNow - time).TotalMilliseconds}ms");
        GC.Collect();
        return archives;
    }

    static void ClearImageBuffer() {
        ImageBuffer[0]?.Dispose();
        ImageBuffer[1]?.Dispose();
        ImageBuffer[0] = null;
        ImageBuffer[1] = null;
        GC.Collect();
    }

    static bool FileIsValidArchive(string file) => file.EndsWith(".zip")
                                                   || file.EndsWith(".cbr")
                                                   || file.EndsWith(".cbz");

    public static async Task UpdateImageBufferToNext(int height, bool refreshAll = false) {
        if (_archive.Length == 0) {
            ClearImageBuffer();
            DebugLogger.Log("Archive is empty");
            return;
        }

        if (CurrentPageIndex >= _archive.Length) {
            ClearImageBuffer();
            DebugLogger.Log($"No more images to load ({CurrentPageIndex + 1}/{_archive.Length})");
            return;
        }

        ImageBuffer[0] = ImageBuffer[1] is null || refreshAll
                             ? await _archive[CurrentPageIndex]?.GetBitmapAsync(height)!
                             : ImageBuffer[1];
        await UpdateBackgroundColor(CurrentPageIndex);
        OnCurrentPageProcessed?.Invoke();

        ImageBuffer[1] = null;
        OnNextPageProcessed?.Invoke();

        ImageBuffer[1] = CurrentPageIndex < _archive.Length - 1
                             ? await _archive[CurrentPageIndex + 1]?.GetBitmapAsync(height)!
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

    public static async Task UpdateImageBufferToPrevious(int height, bool refreshAll = false) {
        ImageBuffer[1] = CurrentPageIndex < _archive.Length - 1 && ImageBuffer[0] is null || refreshAll
                             ? await _archive[CurrentPageIndex + 1]?.GetBitmapAsync(height)!
                             : ImageBuffer[0];
        OnNextPageProcessed?.Invoke();

        ImageBuffer[0] = null;
        OnCurrentPageProcessed?.Invoke();

        ImageBuffer[0] = await _archive[CurrentPageIndex]?.GetBitmapAsync(height)!;
        await UpdateBackgroundColor(CurrentPageIndex);
        OnCurrentPageProcessed?.Invoke();

        Console.WriteLine(ImageBuffer[0] is not null
                              ? $"Loaded {_archive[CurrentPageIndex]?.Name} to main buffer"
                              : "No more images to load");
    }
}