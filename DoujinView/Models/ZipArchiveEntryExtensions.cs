using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DoujinView.Models;

public static class ZipArchiveEntryExtensions {
    public static Avalonia.Media.Color MainColor { get; private set; } = Colors.Black;
    public static Avalonia.Media.Color HeaderColor { get; private set; } = Colors.Black;

    public static async Task<Bitmap?> GetBitmapAsync(this ZipArchiveEntry? entry) {
        if (entry is null) return null;
        await using var stream = entry.Open();
        try {
            using var image = await Image.LoadAsync<Rgb24>(stream);
            MainColor = image.SamplePixel(10, 10);
            HeaderColor = Avalonia.Media.Color.FromRgb((byte)(255 - MainColor.R), (byte)(255 - MainColor.G), (byte)(255 - MainColor.B));
            using var ms = new MemoryStream();
            await image.SaveAsBmpAsync(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        } catch (Exception e) {
            Console.WriteLine(e);
            return null;
        }
    }

    public static bool IsImage(this ZipArchiveEntry? entry) {
        if (entry is null) return false;
        return entry.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
    }

    public static Avalonia.Media.Color SamplePixel(this Image<Rgb24>? image, int x, int y) =>
        image is null ? Colors.Black : image[x, y].ToAvaloniaColor();

    static Avalonia.Media.Color ToAvaloniaColor(this Rgb24 rgb) =>
        Avalonia.Media.Color.FromRgb(rgb.R, rgb.G, rgb.B);
}