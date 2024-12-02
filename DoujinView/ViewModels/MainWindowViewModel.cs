#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
using System;
using Avalonia.Media.Imaging;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using CSharpFunctionalExtensions;
using DoujinView.Models;
using ReactiveUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DoujinView.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    const string APP_NAME = "DoujinView";

    Color                       _backgroundColor = Color.FromRgb(0, 0, 0);
    [ObservableProperty] string _appHeader       = APP_NAME;
    [ObservableProperty] string _pageCounter     = string.Empty;
    [ObservableProperty] bool   _isPaneOpen      = false;

    public static event Action? OnFullScreenToggled;

    static Maybe<Bitmap> CurrentPage    { get; set; }
    static Maybe<Bitmap> NextPage       { get; set; }
    static bool          IsImageLoading { get; set; }

    [RelayCommand]
    void TriggerPane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    public static void GoToNextPage() => NextImage(isJapaneseMode: Settings.JapaneseMode.Value);

    [RelayCommand]
    static void GoToNextPageJump() => NextImage(5, Settings.JapaneseMode.Value);

    [RelayCommand]
    static void GoToFirstPage() => PreviousImage(ImageArchiveManager.TotalPages, Settings.JapaneseMode.Value);

    [RelayCommand]
    public static void GoToPreviousPage() => PreviousImage(isJapaneseMode: Settings.JapaneseMode.Value);

    [RelayCommand]
    static void GoToPreviousPageJump() => PreviousImage(5, Settings.JapaneseMode.Value);

    [RelayCommand]
    static void GoToLastPage() => NextImage(Math.Max(ImageArchiveManager.TotalPages - 1, 0), Settings.JapaneseMode.Value);

    [RelayCommand]
    static void QuitApplication() => Environment.Exit(0);

    [RelayCommand]
    static void OpenNextFile() => ImageArchiveManager.OpenNextFile();

    [RelayCommand]
    static void OpenPreviousFile() => ImageArchiveManager.OpenPreviousFile();

    [RelayCommand]
    static void ToggleFullScreen() => OnFullScreenToggled?.Invoke();

    [RelayCommand]
    public static void OpenArchiveFolder() => ImageArchiveManager.OpenArchiveFolderAndFocus();

    [RelayCommand]
    public static void DeleteArchiveFolder() => ImageArchiveManager.DeleteArchiveFolder();

    static Window? _window;

#pragma warning disable CS0618 // Type or member is obsolete
    public ICommand OpenFileCommand { get; } = ReactiveCommand.CreateFromTask(async () => {
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
        _window = App.MainWindow;
        Initialize();
    }

    public static async void Initialize() => await ImageArchiveManager.Initialize(string.IsNullOrEmpty(App.PathArg)
                                                                                      ? App.GetSetting("LastOpenedFilePath")
                                                                                      : App.PathArg);

    public static async void NextImage(int forwardPages = 1, bool isJapaneseMode = true) {
        if (!isJapaneseMode) {
            PreviousImage(forwardPages);
            return;
        }

        if (IsImageLoading) return;
        IsImageLoading = true;

        if (Settings.LoadNextArchive.Value && ImageArchiveManager.CurrentPageNumber == ImageArchiveManager.TotalPages && forwardPages == 1) {
            await ImageArchiveManager.OpenNextFile();
            IsImageLoading = false;
            return;
        }

        ImageArchiveManager.CurrentPageIndex += forwardPages;
        try {
            await ImageArchiveManager.UpdateImageBufferToNext(App.WindowHeight, forwardPages > 1);
        }
        catch (Exception) {
            Console.WriteLine("Failed to load next image");
        }

        IsImageLoading = false;
    }

    public static async void PreviousImage(int backwardsPages = 1, bool isJapaneseMode = true) {
        if (!isJapaneseMode) {
            NextImage(backwardsPages);
            return;
        }

        if (IsImageLoading) return;
        IsImageLoading = true;

        if (Settings.LoadNextArchive.Value && ImageArchiveManager.CurrentPageIndex == 0 && backwardsPages == 1) {
            await ImageArchiveManager.OpenPreviousFile();
            IsImageLoading = false;
            return;
        }

        ImageArchiveManager.CurrentPageIndex -= backwardsPages;
        try {
            await ImageArchiveManager.UpdateImageBufferToPrevious(App.WindowHeight, backwardsPages > 1);
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

    void LoadNextImage() {
        NextPage = Maybe<Bitmap>.From(ImageArchiveManager.ImageBuffer[1]!);
        NextPage.Match(
                       bitmap => App.MainWindow?.UpdateNextPage(bitmap),
                       () => App.MainWindow?.UpdateNextPage(null)
                      );
    }
}