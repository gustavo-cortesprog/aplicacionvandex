using Foundation;

namespace aplicacionvandex
{
    // iOS carga este delegado al arrancar y desde aquí se crea la aplicación MAUI.
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
