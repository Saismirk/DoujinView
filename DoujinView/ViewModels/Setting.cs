namespace DoujinView.ViewModels;

public class Setting<T> : ISetting {
    private T?     _value;
    public  string Key { get; }

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