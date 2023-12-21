using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DoujinView.Models;

public class StringToLabelConverter : IValueConverter {
    public static readonly StringToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is string sourceString && parameter is string label && typeof(string).IsAssignableTo(targetType)) {
            return $"{label} {sourceString}";
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
}