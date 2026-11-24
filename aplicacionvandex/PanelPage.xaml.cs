using System.Collections.ObjectModel;

namespace aplicacionvandex;

public partial class PanelPage : ContentPage
{
    private readonly ObservableCollection<PosicionMovil> posiciones = new();
    private bool consultando;

    public PanelPage()
    {
        InitializeComponent();
        // ObservableCollection avisa a CollectionView cuando cambia la lista.
        listaPosiciones.ItemsSource = posiciones;
    }

    // Consulta el portafolio cada vez que vuelves a esta pantalla.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Actualizar();
    }

    // Permite repetir la consulta con Actualizar o abrir los ajustes con Conexión.
    private async void OnActualizarClicked(object? sender, EventArgs e) => await Actualizar();
    private async void OnConexionClicked(object? sender, EventArgs e) => await Navigation.PushAsync(new ConexionPage());

    // Bloquea otra consulta mientras carga los datos. Al terminar repone el botón aunque haya fallado la petición.
    private async Task Actualizar()
    {
        if (consultando) return;
        consultando = true;
        btnActualizar.IsEnabled = false;
        cargando.IsVisible = cargando.IsRunning = true;
        try
        {
            var datos = await RadarVantex.Revisar();
            posiciones.Clear();
            foreach (var posicion in datos.Posiciones) posiciones.Add(posicion);
            lblEstado.Text = "Consulta del " + datos.Consulta.ToLocalTime().ToString("dd/MM HH:mm");
            DibujarRadar();
        }
        catch (Exception error)
        {
            posiciones.Clear();
            lblEstado.Text = ConexionVantex.MensajeError(error);
            DibujarRadar();
        }
        finally
        {
            cargando.IsVisible = cargando.IsRunning = false;
            btnActualizar.IsEnabled = true;
            consultando = false;
        }
    }

    // La barra mide la distancia al umbral en el modo elegido, con una etiqueta de texto accesible.
    private void DibujarRadar()
    {
        var ajustes = RadarVantex.Ajustes;
        var mayor = posiciones.Where(p => p.Modo == ajustes.Modo && p.Rendimiento.HasValue)
            .MaxBy(p => Math.Abs(p.Rendimiento!.Value));
        if (mayor is null)
        {
            lblRadar.Text = "Sin posiciones con cotización para el modo de alertas seleccionado.";
            barraRadar.Progress = 0;
            return;
        }

        var cambio = mayor.Rendimiento!.Value;
        lblRadar.Text = $"{mayor.Simbolo} · {cambio:+0.00;-0.00;0.00} % · {mayor.Fuente}";
        if (ajustes.Regla == "rendimiento")
        {
            lblRadar.Text += $" · Umbral ±{ajustes.Umbral} %";
            barraRadar.Progress = Math.Clamp((double)(Math.Abs(cambio) / ajustes.Umbral), 0, 1);
        }
        else
        {
            lblRadar.Text += " · La alerta compara cambios de precio.";
            barraRadar.Progress = 0;
        }
    }
}
