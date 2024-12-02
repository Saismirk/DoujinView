using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.IO;
using SkiaSharp;
using Color = Avalonia.Media.Color;

namespace DoujinView.Models;

public static class ZipArchiveEntryExtensions {
    static readonly RecyclableMemoryStreamManager _memoryStreamManager = new RecyclableMemoryStreamManager();

    public static async Task<Bitmap?> GetBitmapAsync(this ZipArchiveEntry? entry, int height) {
        if (entry is null) return null;
    #if DEBUG
        var time = DateTime.Now;
    #endif
        await using var stream = entry.Open();
        try {
            using var ms = _memoryStreamManager.GetStream("GetBitmapAsync");
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            var bm = Bitmap.DecodeToHeight(ms, height, BitmapInterpolationMode.MediumQuality);
        #if DEBUG
            Console.WriteLine($"GetBitmapAsync: {(DateTime.Now - time).TotalMilliseconds}ms");
        #endif
            GC.Collect();
            return bm;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            return null;
        }
    }

    public static async Task<Color> ReadBackgroundPixel(this ZipArchiveEntry? entry, int x, int y) {
        if (entry is null) return Colors.Black;
        await using var stream = entry.Open();
        try {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, 32);
            ms.Position = 0;
            using var image = SKBitmap.Decode(ms);
            return image.GetPixel(x, y).ToAvaloniaColor();
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }

        return Colors.Black;
    }

    public static string GetFormat(this ZipArchiveEntry? entry) {
        if (entry is null) return string.Empty;
        return entry.FullName.Split('.').Last();
    }

    public static bool IsImage(this ZipArchiveEntry? entry) {
        if (entry is null) return false;
        return entry.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || entry.FullName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    public static Color ToAvaloniaColor(this SKColor color) =>
        Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
}