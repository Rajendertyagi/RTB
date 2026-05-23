using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TB.Core.DI;
using TB.Data.Services;
using TB.Infrastructure;

namespace TB.Core;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        var pathResolver = new PathResolver();
        pathResolver.EnsureDirectories();
        Services = Container.Register(pathResolver);
    }
}
