using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DoujinView.ViewModels;

namespace DoujinView.Views;

public partial class Properties : UserControl {
    public Properties() {
        InitializeComponent();
        DataContext = new ArchivePropertiesViewModel();
    }
}