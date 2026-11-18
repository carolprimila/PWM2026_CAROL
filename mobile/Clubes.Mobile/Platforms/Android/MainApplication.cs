using Android.App;
using Android.Runtime;

namespace Clubes.Mobile;

// HTTP sem TLS (API local de desenvolvimento) é permitido apenas em builds Debug.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application(UsesCleartextTraffic = false)]
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
