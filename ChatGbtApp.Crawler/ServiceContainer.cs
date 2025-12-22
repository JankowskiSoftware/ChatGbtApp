using AutoMapper;
using ChatGbtApp;
using ChatGbtApp.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler;

public static class ServiceContainer
{
    private static IServiceProvider? _provider;

    public static void Configure()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddSingleton<AppDbContext>();
        services.AddSingleton<OpenAiApi>();
        services.AddSingleton<TerminalAgent>();
        services.AddSingleton<JobsCrawler>();
        services.AddSingleton<JobStorage>();
        services.AddSingleton<Chromium>(provider =>
            new Chromium(
                "https://app.loopcv.pro/login",
                provider.GetRequiredService<ILogger<Chromium>>()
            )
        );
        services.AddSingleton<GptKeyValueParser>();
        _provider = services.BuildServiceProvider();
        
        services.AddSingleton<IMapper>(_ => CreateMapper(Resolve<ILoggerFactory>()));
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
    
    private static IMapper CreateMapper(ILoggerFactory loggerFactory){
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<JobBase, Job>();
            cfg.CreateMap<ParsedJobFit, Job>();
        }, loggerFactory);

        return config.CreateMapper();
    }
}
