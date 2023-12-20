using System;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CSharpFunctionalExtensions;
using DoujinView.Models;
using ReactiveUI;

namespace DoujinView.ViewModels;

public static class Settings {
    public const string LAST_OPENED_FILE_PATH = "LastOpenedFilePath";
    public const string JAPANESE_MODE = "JapaneseMode";
    public const string WINDOW_STATE = "WindowState";

    public static Setting<string> LastOpenedFilePath { get; set; } = new(LAST_OPENED_FILE_PATH);
    public static Setting<bool> JapaneseMode { get; set; } = new(JAPANESE_MODE);
    public static Setting<int> WindowState { get; set; } = new(WINDOW_STATE);

    public static void UpdateSettings() {
        JapaneseMode?.Update();
    }
}

public class Setting<T> : ISetting {
    private T? _value;
    public string Key { get; }

    public T? Value {
        get => _value;
        set {
            _value = value;
            App.SaveToAppConfiguration(Key, value?.ToString() ?? string.Empty);
        }
    }

    public Setting(string key) {
        Key = key;
        Update();
    }

    public void Update() {
        Value = App.GetSetting<T>(Key);
    }
}

public interface ISetting {
    public string Key { get; }
    public void Update();
}

public class MainWindowViewModel : ViewModelBase {
    const string APP_NAME = "DoujinView";

    Color _backgroundColor = Color.FromRgb(0, 0, 0);
    string _appHeader = APP_NAME;
    string _pageCounter = string.Empty;

    public static event Action? OnFullScreenToggled;

    public string AppHeader {
        get => _appHeader;
        set => this.RaiseAndSetIfChanged(ref _appHeader, value);
    }

    public string PageCounter {
        get => _pageCounter;
        set => this.RaiseAndSetIfChanged(ref _pageCounter, value);
    }

    public static Maybe<Bitmap> CurrentPage { get; set; }
    public static Maybe<Bitmap> NextPage { get; set; }
    public static bool IsImageLoading { get; private set; }

    public ICommand NextPageCommand { get; } = ReactiveCommand.CreateFromTask(async () => await NextImage(isJapaneseMode: Settings.JapaneseMode.Value));
    public ICommand NextPageJumpCommand { get; } = ReactiveCommand.CreateFromTask(async () => await NextImage(5, Settings.JapaneseMode.Value));
    public ICommand PreviousPageCommand { get; } = ReactiveCommand.CreateFromTask(async () => await PreviousImage(isJapaneseMode: Settings.JapaneseMode.Value));
    public ICommand PreviousPageJumpCommand { get; } = ReactiveCommand.CreateFromTask(async () => await PreviousImage(5, Settings.JapaneseMode.Value));
    public ICommand QuitApplicationCommand { get; } = ReactiveCommand.Create(() => Environment.Exit(0));
    public ICommand OpenNextFileCommand { get; } = ReactiveCommand.CreateFromTask(async () => await ImageArchiveManager.OpenNextFile());
    public ICommand OpenPreviousFileCommand { get; } = ReactiveCommand.Create(ImageArchiveManager.OpenPreviousFile);
    public ICommand ToggleFullScreenCommand { get; } = ReactiveCommand.Create(() => OnFullScreenToggled?.Invoke());

    public ICommand OpenFileCommand { get; } = ReactiveCommand.CreateFromTask(async () =>
    {
        if (IsImageLoading) return;
        if (App.MainWindow is null) return;
        var dialog = new OpenFileDialog();
        dialog.Filters.Add(new FileDialogFilter { Name = "Image Archive", Extensions = { "zip", "7z", "cbz", "cbr" } });
        var result = await dialog.ShowAsync(App.MainWindow);
        if (result is null || result.Length == 0 || string.IsNullOrEmpty(result[0])) return;
        App.PathArg = result[0];
        Initialize();
    });

    public MainWindowViewModel() {
        ImageArchiveManager.OnCurrentPageProcessed += LoadCurrentImage;
        ImageArchiveManager.OnNextPageProcessed += LoadNextImage;
        Initialize();
    }

    public static async void Initialize() => await ImageArchiveManager.Initialize(string.IsNullOrEmpty(App.PathArg)
        ? App.GetSetting("LastOpenedFilePath")
        : App.PathArg);

    public static async Task NextImage(int forwardPages = 1, bool isJapaneseMode = true) {
        if (!isJapaneseMode) {
            await PreviousImage(forwardPages);
            return;
        }

        if (IsImageLoading) return;
        IsImageLoading = true;
        ImageArchiveManager.CurrentPageIndex += forwardPages;
        try {
            await ImageArchiveManager.UpdateImageBufferToNext(forwardPages > 1);
        }
        catch (Exception) {
            ImageArchiveManager.CurrentPageIndex--;
        }

        IsImageLoading = false;
    }

    public static async Task PreviousImage(int backwardsPages = 1, bool isJapaneseMode = true) {
        if (!isJapaneseMode) {
            await NextImage(backwardsPages);
            return;
        }

        if (IsImageLoading) return;
        IsImageLoading = true;
        ImageArchiveManager.CurrentPageIndex -= backwardsPages;
        try {
            await ImageArchiveManager.UpdateImageBufferToPrevious(backwardsPages > 1);
        }
        catch (Exception) {
            ImageArchiveManager.CurrentPageIndex++;
        }

        IsImageLoading = false;
    }

    void LoadCurrentImage() {
        CurrentPage = Maybe<Bitmap>.From(ImageArchiveManager.ImageBuffer[0]!);
        CurrentPage.Match(
            bitmap => App.MainWindow?.UpdateCurrentPage(bitmap),
            () => App.MainWindow?.UpdateCurrentPage(null)
        );

        AppHeader = $"{ImageArchiveManager.ArchiveName} - {ImageArchiveManager.CurrentPageName}";
        PageCounter = $"{ImageArchiveManager.CurrentPageNumber}/{ImageArchiveManager.TotalPages}";
    }

    public void ToggleFullScreen() {
        OnFullScreenToggled?.Invoke();
    }

    void LoadNextImage() {
        NextPage = Maybe<Bitmap>.From(ImageArchiveManager.ImageBuffer[1]!);
        NextPage.Match(
            bitmap => App.MainWindow?.UpdateNextPage(bitmap),
            () => App.MainWindow?.UpdateNextPage(null)
        );
    }
}