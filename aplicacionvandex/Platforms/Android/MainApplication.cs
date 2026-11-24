using Android.App;
using Android.Runtime;

namespace aplicacionvandex
{
    // Android crea esta clase al arrancar el proceso. CreateMauiApp carga la configuración común.
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
