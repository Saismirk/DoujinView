using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CSharpFunctionalExtensions;
using DoujinView.Models;

namespace DoujinView.ViewModels;

public partial class ArchivePropertiesViewModel : ViewModelBase {
    [ObservableProperty] string  _archiveName           = string.Empty;
    [ObservableProperty] string  _archivePath           = string.Empty;
    [ObservableProperty] string  _archiveSize           = string.Empty;
    [ObservableProperty] string  _archiveType           = string.Empty;
    [ObservableProperty] string  _archivePages          = string.Empty;
    [ObservableProperty] string  _currentPageName       = string.Empty;
    [ObservableProperty] string  _currentPageSize       = string.Empty;
    [ObservableProperty] string  _currentPageNumber     = string.Empty;
    [ObservableProperty] string  _currentPageDimensions = string.Empty;
    [ObservableProperty] string  _nextArchiveName       = string.Empty;
    [ObservableProperty] string  _previousArchiveName   = string.Empty;
    [ObservableProperty] Bitmap? _coverUrlBitmap;

    public ArchivePropertiesViewModel() {
        ImageArchiveManager.OnArchiveOpened += UpdateArchiveProperties;
        ImageArchiveManager.OnCurrentPageProcessed += UpdateCurrentPageProperties;
    }

    public void UpdateArchiveProperties() {
        PreviousArchiveName = ArchiveName;
        ArchiveName = ImageArchiveManager.ArchiveName;
        ArchivePath = ImageArchiveManager.ArchivePath;
        ArchiveSize = ImageArchiveManager.ArchiveSize;
        ArchiveType = ImageArchiveManager.ArchiveType;
        ArchivePages = ImageArchiveManager.ArchivePages.ToString();
        NextArchiveName = ImageArchiveManager.NextArchiveName;
        PreviousArchiveName = ImageArchiveManager.PreviousArchiveName;
    }

    void UpdateCurrentPageProperties() {
        CurrentPageName = ImageArchiveManager.CurrentPageName;
        CurrentPageSize = ImageArchiveManager.CurrentPageSize;
        CurrentPageNumber = ImageArchiveManager.CurrentPageNumber.ToString();
        CurrentPageDimensions = $"{ImageArchiveManager.CurrentPageWidth}x{ImageArchiveManager.CurrentPageHeight}";
    }
}