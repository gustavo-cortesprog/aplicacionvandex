namespace aplicacionvandex;

public partial class App : Application
{
    public App()
    {
        // Carga la paleta y los estilos compartidos declarados en App.xaml.
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // El examen inicia con una FlyoutPage. Aquí se conserva la misma navegación.
        var ventana = new Window(new MainPage()) { Title = "Vantex Móvil" };
        // El reloj programa la revisión en el hilo de la aplicación. La bandera evita iniciar otra antes de que termine.
        var reloj = Dispatcher.CreateTimer();
        reloj.Interval = TimeSpan.FromSeconds(30);
        bool revisando = false;
        reloj.Tick += async (_, _) =>
        {
            if (revisando || !RadarVantex.Ajustes.Activas) return;
            revisando = true;
            try { await RadarVantex.Revisar(); }
            catch (Exception error) { System.Diagnostics.Debug.WriteLine(error.GetType().Name); }
            finally { revisando = false; }
        };

        // La revisión frecuente funciona con la app visible. Android programa la suya aparte.
        ventana.Activated += (_, _) => reloj.Start();
        ventana.Deactivated += (_, _) => reloj.Stop();
        ventana.Destroying += (_, _) => reloj.Stop();
        return ventana;
    }
}
