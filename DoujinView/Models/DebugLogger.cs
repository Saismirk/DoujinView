using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Notification;

namespace DoujinView.Models;

public static class DebugLogger {

    static INotificationMessageManager NotificationManager { get; } = new NotificationMessageManager();
    public static void Log(string message, bool showNotification = false) {
        Console.WriteLine(message);
        if (!showNotification) return;
        NotificationManager.CreateMessage()
                           .Accent("#1751C3")
                           .Animates(true)
                           .Background("#333")
                           .HasBadge("Log")
                           .HasMessage(message)
                           .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                           .Queue();
    }
}