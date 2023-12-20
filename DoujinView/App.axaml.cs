using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DoujinView.ViewModels;
using DoujinView.Views;

namespace DoujinView;

public partial class App : Application {
    public static MainWindow? MainWindow { get; private set; }
    public static string PathArg { get; set; } = string.Empty;

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var args = Environment.GetCommandLineArgs();

            if (args.Length > 1) {
                PathArg = args[1];
            }
            
            desktop.MainWindow = new MainWindow {
                DataContext = new MainWindowViewModel(),
            };

            MainWindow = desktop.MainWindow as MainWindow;

        }

        base.OnFrameworkInitializationCompleted();
    }
}