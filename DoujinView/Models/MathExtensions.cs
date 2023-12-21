using System;
using System.Globalization;

namespace DoujinView.Models;

public static class MathExtensions {
    public static string ToFileSize(this long size) {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        if (size == 0) return $"0{suf[0]}";
        var bytes = Math.Abs(size);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num   = Math.Round(bytes / Math.Pow(1024, place), 1);
        return $"{(Math.Sign(size) * num).ToString(CultureInfo.InvariantCulture)}{suf[place]}";
    }
}