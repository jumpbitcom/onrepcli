using Avalonia.Controls;
using OnRepCli.ViewModels;

namespace OnRepCli.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
