namespace aplicacionvandex;

public partial class ConexionPage : ContentPage
{
    private bool ocupado;

    // Carga la dirección guardada y muestra instrucciones según el sistema y el tipo de compilación.
    public ConexionPage()
    {
        InitializeComponent();
        entryServidor.Text = ConexionVantex.Direccion;
#if DEBUG
        lblAyuda.Text = DeviceInfo.Platform == DevicePlatform.Android
            ? "Emulador Android: http://10.0.2.2:5175. Teléfono por USB: http://127.0.0.1:5175 con el túnel ADB. Para otros servidores usa HTTPS."
            : "En esta computadora puedes usar http://localhost:5175. Para otros servidores usa HTTPS.";
#else
        lblAyuda.Text = "Escribe la dirección HTTPS del servidor que aloja Vantex.";
#endif
    }

    // Muestra el botón para cerrar la vinculación si el dispositivo ya tiene una cuenta.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            btnDesvincular.IsVisible = await ConexionVantex.ObtenerAcceso() is not null;
            if (btnDesvincular.IsVisible) lblEstado.Text = "Portafolio vinculado.";
        }
        catch (Exception error) { lblEstado.Text = ConexionVantex.MensajeError(error); }
    }

    // Verifica el servidor antes de guardar su dirección. Si cambió de origen, elimina el acceso anterior.
    private async void OnProbarClicked(object? sender, EventArgs e)
    {
        if (ocupado || !ObtenerServidor(out var direccion)) return;
        EstablecerOcupado(true);
        try
        {
            await ConexionVantex.Probar(direccion, CancellationToken.None);
            var anterior = await ConexionVantex.ObtenerAcceso();
            if (anterior is not null && anterior.Direccion != direccion.GetLeftPart(UriPartial.Authority))
            {
                ConexionVantex.Desvincular();
                await LimpiarRadar();
                btnDesvincular.IsVisible = false;
            }
            ConexionVantex.GuardarDireccion(direccion);
            lblEstado.Text = "Servidor Vantex confirmado y guardado.";
        }
        catch (Exception error) { lblEstado.Text = ConexionVantex.MensajeError(error); }
        finally { EstablecerOcupado(false); }
    }

    // Valida el formato del código y lo canjea por el acceso de lectura.
    private async void OnVincularClicked(object? sender, EventArgs e)
    {
        if (ocupado || !ObtenerServidor(out var direccion)) return;
        var codigo = entryCodigo.Text?.Trim().Replace("-", "") ?? "";
        if (codigo.Length != 10 || !codigo.All(Uri.IsHexDigit))
        {
            lblEstado.Text = "Escribe los 10 caracteres del código generado en Vantex.";
            return;
        }

        EstablecerOcupado(true);
        try
        {
            await ConexionVantex.Vincular(direccion, codigo, CancellationToken.None);
            // La cuenta puede haber cambiado, así que se descartan referencias de la anterior.
            await LimpiarRadar();
            entryCodigo.Text = "";
            btnDesvincular.IsVisible = true;
            lblEstado.Text = "Portafolio vinculado. Ya puedes consultar posiciones y configurar alertas.";
        }
        catch (Exception error) { lblEstado.Text = ConexionVantex.MensajeError(error); }
        finally { EstablecerOcupado(false); }
    }

    // Abre directamente la sección web donde se genera el código.
    private async void OnAbrirWebClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new WebPage("#portafolio"));

    // Pide confirmación antes de retirar del dispositivo la cuenta y el historial del radar.
    private async void OnDesvincularClicked(object? sender, EventArgs e)
    {
        if (ocupado) return;
        if (!await DisplayAlertAsync("Desvincular portafolio", "Se borrarán el acceso y el historial de alertas de este dispositivo.", "Desvincular", "Cancelar"))
            return;
        EstablecerOcupado(true);
        try
        {
            ConexionVantex.Desvincular();
            await LimpiarRadar();
            btnDesvincular.IsVisible = false;
            lblEstado.Text = "Portafolio desvinculado.";
        }
        catch (Exception error) { lblEstado.Text = ConexionVantex.MensajeError(error); }
        finally { EstablecerOcupado(false); }
    }

    // Valida la dirección escrita y muestra una indicación si es incorrecta.
    private bool ObtenerServidor(out Uri servidor)
    {
        if (ConexionVantex.LeerDireccion(entryServidor.Text, out servidor)) return true;
        lblEstado.Text = "Escribe el origen de Vantex con HTTPS, o una dirección local de depuración.";
        return false;
    }

    // Pausa la vigilancia, borra referencias e historial y cancela la tarea de Android.
    private static async Task LimpiarRadar()
    {
        var ajustes = RadarVantex.Ajustes;
        ajustes.Activas = false;
        await RadarVantex.GuardarAjustes(ajustes);
        await RadarVantex.Reiniciar();
        NotificacionesVantex.Programar(false);
    }

    // Bloquea los botones y muestra el indicador mientras la pantalla espera una respuesta.
    private void EstablecerOcupado(bool valor)
    {
        ocupado = valor;
        btnProbar.IsEnabled = btnVincular.IsEnabled = btnDesvincular.IsEnabled = btnAbrirWeb.IsEnabled = !valor;
        cargando.IsVisible = cargando.IsRunning = valor;
    }
}
