using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace DoujinView.ViewModels;

public partial class SettingsViewModel : ViewModelBase {
    [ObservableProperty] bool _japaneseMode      = Settings.JapaneseMode.Value;
    [ObservableProperty] bool _resumeLastArchive = Settings.ResumeLastArchive.Value;
    [ObservableProperty] bool _resumeLastPage    = Settings.ResumeLastPage.Value;
    [ObservableProperty] bool _loadNextArchive   = Settings.LoadNextArchive.Value;
    [ObservableProperty] bool _creationOrder     = Settings.CreationOrder.Value;

    public bool JapaneseModeSetting {
        get => JapaneseMode;
        set {
            Settings.JapaneseMode.Value = value;
            Settings.JapaneseMode.Update();
            JapaneseMode = value;
        }
    }

    public bool CreationOrderSetting {
        get => CreationOrder;
        set {
            Settings.CreationOrder.Value = value;
            Settings.CreationOrder.Update();
            CreationOrder = value;
        }
    }

    public bool ResumeLastArchiveSetting {
        get => ResumeLastArchive;
        set {
            Settings.ResumeLastArchive.Value = value;
            Settings.ResumeLastArchive.Update();
            ResumeLastArchive = value;
        }
    }

    public bool ResumeLastPageSetting {
        get => ResumeLastPage;
        set {
            Settings.ResumeLastPage.Value = value;
            Settings.ResumeLastPage.Update();
            ResumeLastPage = value;
        }
    }

    public bool LoadNextArchiveSetting {
        get => LoadNextArchive;
        set {
            Settings.LoadNextArchive.Value = value;
            Settings.LoadNextArchive.Update();
            LoadNextArchive = value;
        }
    }
}