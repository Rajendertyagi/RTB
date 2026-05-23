using Microsoft.Extensions.DependencyInjection;
using TB.Data.Services;
using TB.Features.Navigation;
using TB.Features.Tabs;
using TB.Infrastructure;

namespace TB.Core.DI;

public static class Container
{
    public static IServiceProvider Register(PathResolver pathResolver)
    {
        var services = new ServiceCollection();
        services.AddSingleton(pathResolver);
        services.AddSingleton<SettingsService>();
        services.AddSingleton<NavigationViewModel>();
        services.AddSingleton<TabService>();
        return services.BuildServiceProvider();
    }
}
