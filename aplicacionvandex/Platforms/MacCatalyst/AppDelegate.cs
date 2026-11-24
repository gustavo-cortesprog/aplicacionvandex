using Foundation;

namespace aplicacionvandex
{
    // Mac Catalyst usa este delegado para iniciar la misma configuración MAUI.
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
