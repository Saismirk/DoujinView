using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DoujinView.Models;
using DoujinView.ViewModels;

namespace DoujinView.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        AttachFullScreenHandler();

        if (Settings.WindowState.Value == (int)WindowState.FullScreen) {
            ToggleFullScreen();
        }
    }

    public void UpdateCurrentPage(Bitmap? bitmap) {
        if (bitmap is null) {
            CurrentImage.Source = null;
            return;
        }

        CurrentImage.Source = bitmap;
        var ratio = (float)bitmap.PixelSize.Width / bitmap.PixelSize.Height;
        FitImageToWindow(CurrentImage, ratio);
        MainPanel.Background = new SolidColorBrush(ImageArchiveManager.MainColor);
        AppHeader.Foreground = new SolidColorBrush(ImageArchiveManager.HeaderColor);
        PageCounter.Foreground = new SolidColorBrush(ImageArchiveManager.HeaderColor);
        NextImage.IsVisible = ratio < 1;
    }

    public void UpdateNextPage(Bitmap? bitmap) {
        if (bitmap is null) {
            NextImage.Source = null;
            return;
        }

        NextImage.Source = bitmap;
        var ratio = (float)bitmap.PixelSize.Width / bitmap.PixelSize.Height;
        FitImageToWindow(NextImage, ratio);
        if (ratio >= 1) {
            NextImage.IsVisible = false;
        }
    }

    public void FitImageToWindow(Image image, float ratio) {
        image.Height = Height;
        image.Width = Height * ratio;
    }

    async void OnMouseWheelChanged(object sender, PointerWheelEventArgs e) {
        if (e.Delta.Y > 0) {
            MainWindowViewModel.GoToPreviousPage();
        } else if (e.Delta.Y < 0) {
            MainWindowViewModel.GoToNextPage();
        }

        e.Handled = true;
    }

    void ToggleFullScreen() {
        var isFullScreen = WindowState == WindowState.FullScreen;
        WindowState = isFullScreen ? WindowState.Normal : WindowState.FullScreen;
        AppHeader.IsVisible = isFullScreen;
        ExtendClientAreaToDecorationsHint = isFullScreen;
        Settings.WindowState.Value = (int)WindowState;
    }

    void AttachFullScreenHandler() {
        MainWindowViewModel.OnFullScreenToggled += ToggleFullScreen;
    }
}