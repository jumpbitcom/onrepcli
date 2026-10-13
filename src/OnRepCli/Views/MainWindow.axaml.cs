using Avalonia.Controls;
using OnRepCli.ViewModels;

namespace OnRepCli.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await viewModel.RefreshAsync();
    }
}
