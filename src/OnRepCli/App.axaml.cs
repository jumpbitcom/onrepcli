using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OnRepCli.Services;
using OnRepCli.ViewModels;
using OnRepCli.Views;

namespace OnRepCli;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
            services.AddSingleton<NewsPageParser>();
            services.AddSingleton<INewsService, OnlineReportsNewsService>();
            services.AddSingleton<MainViewModel>();
            var serviceProvider = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow(serviceProvider.GetRequiredService<MainViewModel>());
            desktop.Exit += (_, _) => serviceProvider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
