using Microsoft.Extensions.Logging;

using System.Globalization;

namespace VanderBakery.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
