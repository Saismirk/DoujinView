using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DoujinView.ViewModels;
using DoujinView.Views;

namespace DoujinView;

public partial class App : Application {
    public static MainWindow?    MainWindow       { get; private set; }
    public static string         PathArg          { get; set; } = string.Empty;
    public static Configuration? AppConfiguration { get; private set; }

    public override void Initialize() {
        AppConfiguration = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
        AvaloniaXamlLoader.Load(this);
    }

    public static int WindowHeight => MainWindow != null ? (int)MainWindow.Height : 1080;

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

    public static void SaveToAppConfiguration(string key, string value) {
        try {
            var settings = AppConfiguration?.AppSettings.Settings;
            if (settings?[key] is null) {
                settings?.Add(key, value);
            }
            else {
                settings[key].Value = value;
            }

            AppConfiguration!.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection(AppConfiguration.AppSettings.SectionInformation.Name);
        }
        catch (ConfigurationErrorsException e) {
            Console.WriteLine("Error writing app settings");
        }
    }

    public static string GetSetting(string key) {
        try {
            var settings = AppConfiguration?.AppSettings.Settings;
            return settings?[key]?.Value ?? string.Empty;
        }
        catch (ConfigurationErrorsException) {
            Console.WriteLine("Error reading app settings");
            return string.Empty;
        }
    }

    public static T? GetSetting<T>(string key) {
        try {
            var settings = AppConfiguration?.AppSettings.Settings;
            var value = settings?[key]?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(value)) throw new ConfigurationErrorsException();
            var result = (T)Convert.ChangeType(value, typeof(T));
            Console.WriteLine($"Loaded {key} = {result}");
            return result;
        }
        catch (ConfigurationErrorsException) {
            Console.WriteLine("Error reading app settings");
            return default;
        }
    }
}