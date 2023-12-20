using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace DoujinView.Models;

public static class ImageArchiveManager {
    public static Bitmap?[] ImageBuffer { get; } = new Bitmap?[2];
    public static string ArchiveName       => Path.GetFileNameWithoutExtension(App.PathArg);
    public static string CurrentPageName   => _archive[CurrentPageIndex]?.Name ?? string.Empty;
    public static int    CurrentPageNumber => CurrentPageIndex + 1;
    public static int    TotalPages        => _archive.Length;

    static ZipArchiveEntry?[] _archive          = Array.Empty<ZipArchiveEntry>();
    static int                _currentPageIndex = 0;

    public static event Action? OnCurrentPageProcessed;
    public static event Action? OnNextPageProcessed;

    public static int CurrentPageIndex {
        get => _currentPageIndex;
        set => _currentPageIndex = Math.Clamp(value, 0, _archive.Length - 1);
    }

    public static async Task Initialize(string path) {
        if (string.IsNullOrEmpty(path) || !Path.Exists(path)) return;

        Console.WriteLine($"Loading {path}...");
        try {
            _archive = ZipFile.Open(path, ZipArchiveMode.Read).Entries
                              .Where(entry => entry.IsImage())
                              .ToArray();
        }
        catch (Exception e) {
            Console.WriteLine("Failed to load archive: " + path);
            return;
        }

        ClearImageBuffer();
        CurrentPageIndex = 0;
        await UpdateImageBufferToNext();
    }

    public static async Task OpenNextFile() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var files = Directory.GetFiles(Path.GetDirectoryName(App.PathArg) ?? string.Empty)
                             .Where(file => file.EndsWith(".zip") || file.EndsWith(".cbr") || file.EndsWith(".cbz"))
                             .ToArray();
        var index = Array.IndexOf(files, App.PathArg);
        if (index < files.Length - 1) {
            App.PathArg = files[index + 1];
            await Initialize(App.PathArg);
        }
    }

    public static async void OpenPreviousFile() {
        if (string.IsNullOrEmpty(App.PathArg) || !Path.Exists(App.PathArg)) return;
        var files = Directory.GetFiles(Path.GetDirectoryName(App.PathArg) ?? string.Empty)
                             .Where(file => file.EndsWith(".zip") || file.EndsWith(".cbr") || file.EndsWith(".cbz"))
                             .ToArray();
        var index = Array.IndexOf(files, App.PathArg);
        if (index > 0) {
            App.PathArg = files[index - 1];
            await Initialize(App.PathArg);
        }
    }

    static void ClearImageBuffer() {
        ImageBuffer[0]?.Dispose();
        ImageBuffer[1]?.Dispose();
        ImageBuffer[0] = null;
        ImageBuffer[1] = null;
    }

    public static async Task UpdateImageBufferToNext(bool refreshAll = false) {
        ImageBuffer[0] = ImageBuffer[1] is null || refreshAll
                             ? await _archive[CurrentPageIndex]?.GetBitmapAsync()!
                             : ImageBuffer[1];
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

    public static async Task UpdateImageBufferToPrevious(bool refreshAll = false) {
        ImageBuffer[1] = CurrentPageIndex < _archive.Length - 1 && ImageBuffer[0] is null || refreshAll
                             ? await _archive[CurrentPageIndex + 1]?.GetBitmapAsync()!
                             : ImageBuffer[0];
        OnNextPageProcessed?.Invoke();

        ImageBuffer[0] = null;
        OnCurrentPageProcessed?.Invoke();

        ImageBuffer[0] = await _archive[CurrentPageIndex]?.GetBitmapAsync()!;
        OnCurrentPageProcessed?.Invoke();

        Console.WriteLine(ImageBuffer[0] is not null
                              ? $"Loaded {_archive[CurrentPageIndex]?.Name} to main buffer"
                              : "No more images to load");
    }
}