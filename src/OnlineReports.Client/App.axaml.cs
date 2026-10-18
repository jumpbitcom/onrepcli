using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OnlineReports.Client.Platform;
using OnlineReports.Client.Services;
using OnlineReports.Client.ViewModels;
using OnlineReports.Client.Views;
using System.Net.Http;

namespace OnlineReports.Client;

public partial class App : Application
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var archiveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OnlineReports");
            if (OperatingSystem.IsMacOS())
            {
                MacDockIcon.Apply(Path.Combine(AppContext.BaseDirectory, "Assets", "jumpbit-app.icns"));
            }

            var newsService = new NewsService(_httpClient, archiveDirectory);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(newsService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}