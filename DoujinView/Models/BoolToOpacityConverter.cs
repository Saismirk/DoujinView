using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DoujinView.Models;

public class BoolToDoubleConverter : IValueConverter {
    public static readonly BoolToDoubleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool sourceBool && typeof(double).IsAssignableTo(targetType)) {
            return sourceBool ? 1 : (double)0;
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is double sourceDouble && targetType.IsAssignableTo(typeof(int))) {
            return sourceDouble > 0;
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
}