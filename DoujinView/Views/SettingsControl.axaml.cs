using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DoujinView.ViewModels;

namespace DoujinView.Views; 

public partial class SettingsControl : UserControl {
    public SettingsControl() {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}