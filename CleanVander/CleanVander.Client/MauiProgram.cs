using CleanVander.Client.Services;
using Microsoft.Extensions.Logging;
namespace CleanVander.Client;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp() { var builder=MauiApp.CreateBuilder(); builder.UseMauiApp<App>().ConfigureFonts(fonts=>{fonts.AddFont("OpenSans-Regular.ttf","OpenSansRegular");fonts.AddFont("OpenSans-Semibold.ttf","OpenSansSemibold");});
#if ANDROID
        var baseUrl="http://10.0.2.2:5080";
#else
        var baseUrl="http://localhost:5080";
#endif
        builder.Services.AddSingleton(new HttpClient { BaseAddress=new Uri(baseUrl),Timeout=TimeSpan.FromSeconds(20) }); builder.Services.AddSingleton<CleanVanderApi>(); builder.Services.AddSingleton<LocalStore>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build(); }
}

