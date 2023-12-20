using ReactiveUI;

namespace DoujinView.ViewModels;

public class SettingsViewModel : ViewModelBase {
    bool _japaneseMode = Settings.JapaneseMode.Value;
    public bool JapaneseMode {
        get => _japaneseMode;
        set {
            Settings.JapaneseMode.Value = value;
            Settings.UpdateSettings();
            this.RaiseAndSetIfChanged(ref _japaneseMode, value);
        }
    }
}