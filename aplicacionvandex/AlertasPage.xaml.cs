using System.Collections.ObjectModel;

namespace aplicacionvandex;

public partial class AlertasPage : ContentPage
{
    private readonly ObservableCollection<AvisoVantex> avisos = new();
    private bool ocupado;

    // Llena los selectores y enlaza la lista visual con la colección de avisos.
    public AlertasPage()
    {
        InitializeComponent();
        pickerRegla.ItemsSource = new[] { "Ganancia o pérdida", "Subida o bajada del precio" };
        pickerModo.ItemsSource = new[] { "Demostración", "Mercado" };
        listaAlertas.ItemsSource = avisos;
    }

    // Recupera los ajustes guardados y presenta el historial al entrar.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        var ajustes = RadarVantex.Ajustes;
        swVigilar.IsToggled = ajustes.Activas;
        pickerRegla.SelectedIndex = ajustes.Regla == "precio" ? 1 : 0;
        pickerModo.SelectedIndex = ajustes.Modo == "real" ? 1 : 0;
        sliderUmbral.Value = ajustes.Umbral;
        lblUmbral.Text = $"Umbral ±{ajustes.Umbral} %";
        lblEstado.Text = DeviceInfo.Platform == DevicePlatform.Android
            ? "Android revisa en segundo plano desde cada 15 minutos, según la batería y la red."
            : "El radar revisa mientras Vantex Móvil está abierto.";
        MostrarHistorial();
    }

    // Muestra el porcentaje redondeado mientras se mueve el control.
    private void OnUmbralChanged(object? sender, ValueChangedEventArgs e)
    {
        if (lblUmbral is not null) lblUmbral.Text = $"Umbral ±{Math.Round(e.NewValue)} %";
    }

    // Cambia la explicación según la referencia elegida para comparar.
    private void OnReglaChanged(object? sender, EventArgs e)
    {
        if (lblRegla is null) return;
        lblRegla.Text = pickerRegla.SelectedIndex == 1
            ? "Compara el precio con la primera lectura o el último aviso."
            : "Compara el valor de la posición con el costo de compra.";
    }

    // Guarda la regla, solicita permiso al usuario y deja una primera lectura de referencia.
    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (ocupado) return;
        EstablecerOcupado(true);
        try
        {
            var ajustes = new AjustesAlertas
            {
                Activas = swVigilar.IsToggled,
                Modo = pickerModo.SelectedIndex == 1 ? "real" : "demo",
                Regla = pickerRegla.SelectedIndex == 1 ? "precio" : "rendimiento",
                Umbral = (int)Math.Round(sliderUmbral.Value)
            };
            if (ajustes.Activas && await ConexionVantex.ObtenerAcceso() is null)
                throw new InvalidOperationException("Vincula tu portafolio en Conexión para activar la vigilancia.");

            bool permitido = ajustes.Activas && await NotificacionesVantex.PedirPermiso();
            await RadarVantex.GuardarAjustes(ajustes);
            bool programado = NotificacionesVantex.Programar(ajustes.Activas && permitido);
            lblEstado.Text = !ajustes.Activas ? "Alertas pausadas."
                : DeviceInfo.Platform == DevicePlatform.Android && permitido && programado
                    ? "Alertas activadas en el teléfono. Android decide cuándo ejecuta cada revisión."
                    : "Radar activo mientras la app está abierta. Los avisos quedan en el historial.";

            if (ajustes.Activas) await RadarVantex.Revisar();
            MostrarHistorial();
        }
        catch (Exception error) { lblEstado.Text = ConexionVantex.MensajeError(error); }
        finally { EstablecerOcupado(false); }
    }

    // Solicita una lectura inmediata y vuelve a presentar el historial.
    private async void OnRevisarClicked(object? sender, EventArgs e)
    {
        if (ocupado) return;
        EstablecerOcupado(true);
        try
        {
            await RadarVantex.Revisar();
            lblEstado.Text = "Portafolio revisado.";
            MostrarHistorial();
        }
        catch (Exception error) { lblEstado.Text = ConexionVantex.MensajeError(error); }
        finally { EstablecerOcupado(false); }
    }

    // Ensaya un aviso sin usar cotizaciones. Si el sistema no lo admite, abre un diálogo de demostración.
    private async void OnProbarClicked(object? sender, EventArgs e)
    {
        bool permiso = await NotificacionesVantex.PedirPermiso();
        if (permiso && NotificacionesVantex.Mostrar("Vantex · prueba", "Así se verá una alerta de tu portafolio virtual."))
            lblEstado.Text = "Notificación de prueba solicitada.";
        else
            await DisplayAlertAsync("Prueba del radar", "Aquí se mostrará el aviso cuando tu teléfono permita notificaciones.", "Entendido");
    }

    // Copia al listado los avisos que el radar conserva en el dispositivo.
    private void MostrarHistorial()
    {
        avisos.Clear();
        foreach (var aviso in RadarVantex.Historial) avisos.Add(aviso);
        // El mensaje vacío queda en el encabezado sin tapar los controles de la lista.
        lblSinAvisos.IsVisible = avisos.Count == 0;
    }

    // Desactiva los botones de guardar y revisar mientras una de esas acciones está en curso.
    private void EstablecerOcupado(bool valor)
    {
        ocupado = valor;
        btnGuardar.IsEnabled = btnRevisar.IsEnabled = !valor;
    }
}
