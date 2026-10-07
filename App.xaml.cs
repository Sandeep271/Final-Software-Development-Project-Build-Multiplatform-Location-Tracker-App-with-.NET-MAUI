using Microsoft.Extensions.DependencyInjection;

namespace LocationHeatMapApp;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        // Load application resources before creating MainPage.
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolve MainPage only after App.xaml resources are available.
        var mainPage = _services.GetRequiredService<MainPage>();
        return new Window(mainPage);
    }
}
