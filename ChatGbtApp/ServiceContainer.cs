using Microsoft.Extensions.DependencyInjection;
using ChatGbtApp.Repository;

namespace ChatGbtApp;

public static class ServiceContainer
{
    private static IServiceProvider? _provider;

    public static void Configure()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddScoped<AppDbContext>();
        services.AddSingleton<OpenAiApi>();
        services.AddSingleton<TerminalAgent>();

        _provider = services.BuildServiceProvider();
    }

    public static T Resolve<T>() where T : class
    {
        if (_provider == null)
            throw new InvalidOperationException("ServiceContainer not configured. Call Configure() first.");

        return _provider.GetRequiredService<T>();
    }

    public static object Resolve(Type type)
    {
        if (_provider == null)
            throw new InvalidOperationException("ServiceContainer not configured. Call Configure() first.");

        return _provider.GetRequiredService(type);
    }
}
