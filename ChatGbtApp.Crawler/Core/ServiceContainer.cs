using ChatGbtApp;
using ChatGbtApp.Repository;
using ChatGbtApp.Interfaces;
using ChatGbtApp.Services;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Extractors.Loopcv;
using ChatGgtApp.Crawler.Parsers;
using ChatGgtApp.Crawler.Progress;
using ChatGgtApp.Crawler.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

public static class ServiceContainer
{
    private static IServiceProvider? _provider;

    public static void Configure()
    {
        var services = new ServiceCollection();

        AddLogging(services);
        
        services.AddSingleton<JobProcessingProgress>();

        // HttpClient factory and named client for OpenAI
        services.AddHttpClient("OpenAI", client => { client.Timeout = TimeSpan.FromMinutes(3); });

        services.AddTransient<AppDbContext>(); 
        services.AddTransient<JobStorage>();
        services.AddTransient<StringKeyValueParser>();
        services.AddTransient<Prompt>();

        // OpenAI API
        services.AddTransient<IResponseParser, OpenAiResponseParser>();
        services.AddTransient<OpenAiApiFactory>();
        services.AddTransient<IOpenAiApi>(provider =>
            provider.GetRequiredService<OpenAiApiFactory>().Create());
        services.AddScoped<JobProcessor>();
        services.AddTransient<JobsCrawler>();
        services.AddTransient<MatchesExtractor>();
        services.AddTransient<MatchesCrawler>();
        services.AddTransient<ChromiumFactory>();
        
        // Loopcv login service
        services.AddTransient<LoopCvLogger>(provider =>
            new LoopCvLogger(
                provider.GetRequiredService<ILogger<LoopCvLogger>>(),
                LoopcvConst.Email,
                LoopcvConst.Password,
                LoopcvConst.LoginUrl
            )
        );
        
        _provider = services.BuildServiceProvider();
    }

    private static void AddLogging(ServiceCollection services)
    {
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