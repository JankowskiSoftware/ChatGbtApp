using AutoMapper;
using ChatGbtApp;
using ChatGbtApp.Repository;
using ChatGbtApp.Interfaces;
using ChatGbtApp.Services;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Extractors;
using ChatGgtApp.Crawler.Interfaces;
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
        
        // HttpClient factory and named client for OpenAI
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });
        
        services.AddSingleton<AppDbContext>();
        services.AddSingleton<JobStorage>();
        services.AddSingleton<GptKeyValueParser>();
        services.AddSingleton<JobProcessingProgress>();
        services.AddSingleton<Prompt>();
        
        // OpenAI API
        services.AddSingleton<IResponseParser, OpenAiResponseParser>();
        services.AddSingleton<OpenAiApiFactory>();
        services.AddSingleton<IOpenAiApi>(provider =>
            provider.GetRequiredService<OpenAiApiFactory>().Create());
        services.AddSingleton<IJobProcessor, JobProcessor>();
        services.AddSingleton<JobsCrawler>();
        
        
        
        
        services.AddSingleton<IMapper>(_ => CreateMapper(Resolve<ILoggerFactory>()));
        
        // Register Chromium browser
        services.AddSingleton<Chromium>(provider =>
            new Chromium(
                "https://app.loopcv.pro/login",
                provider.GetRequiredService<ILogger<Chromium>>()
            )
        );
        
        // Register page content extractor
        services.AddSingleton<IPageContentExtractor, ChromiumPageExtractor>(provider =>
            new ChromiumPageExtractor(
                provider.GetRequiredService<Chromium>(),
                provider.GetRequiredService<ILogger<ChromiumPageExtractor>>(),
                urlMatcher: url => url.Contains("loopcv.pro", StringComparison.OrdinalIgnoreCase)
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
    
    private static IMapper CreateMapper(ILoggerFactory loggerFactory){
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<JobBase, Job>();
            cfg.CreateMap<ParsedJobFit, Job>();
        }, loggerFactory);

        return config.CreateMapper();
    }
}
