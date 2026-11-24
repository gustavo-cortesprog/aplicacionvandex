namespace aplicacionvandex;

public partial class WebPage : ContentPage
{
    private readonly string destino;
    private Uri? servidor;

    // Cuando no se indica una sección, abre el inicio de Vantex.
    public WebPage() : this("") { }

    public WebPage(string destino)
    {
        InitializeComponent();
        this.destino = destino;
    }

    // Abre la página al entrar por primera vez y conserva la navegación al regresar.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (servidor is null) Cargar();
    }

    // Combina el servidor guardado con la sección solicitada y se la entrega al WebView.
    private void Cargar()
    {
        if (!ConexionVantex.LeerDireccion(ConexionVantex.Direccion, out var direccion))
        {
            MostrarEstado("Guarda la dirección de Vantex en Conexión para abrir la web.");
            return;
        }

        servidor = direccion;
        webVantex.Source = new Uri(direccion, destino).AbsoluteUri;
    }

    // Retrocede en el historial del sitio dentro de la app.
    private void OnVolverClicked(object? sender, EventArgs e)
    {
        if (webVantex.CanGoBack) webVantex.GoBack();
    }

    // Recarga la página actual o intenta abrirla si todavía no hay una dirección asignada.
    private void OnActualizarClicked(object? sender, EventArgs e)
    {
        if (webVantex.Source is null) Cargar();
        else webVantex.Reload();
    }

    // El sitio de Vantex queda dentro de la app; los enlaces externos usan el navegador del teléfono.
    private async void OnNavegando(object? sender, WebNavigatingEventArgs e)
    {
        if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var url) || servidor is null)
        {
            e.Cancel = true;
            return;
        }
        if (url.Scheme == servidor.Scheme && url.Host == servidor.Host && url.Port == servidor.Port)
        {
            MostrarEstado("Cargando Vantex…");
            return;
        }
        e.Cancel = true;
        if (url.Scheme is "https" or "http")
        {
            try { await Launcher.Default.OpenAsync(url); }
            catch (Exception error) { MostrarEstado(ConexionVantex.MensajeError(error)); }
        }
    }

    // Quita el estado de carga cuando termina o muestra cómo revisar la conexión.
    private void OnNavegado(object? sender, WebNavigatedEventArgs e)
    {
        MostrarEstado(e.Result == WebNavigationResult.Success
            ? "" : "No abrió Vantex. Revisa la dirección, la red y que el servidor esté ejecutándose.");
    }

    // Oculta la etiqueta de estado cuando no hay un mensaje que mostrar.
    private void MostrarEstado(string texto)
    {
        lblEstado.Text = texto;
        lblEstado.IsVisible = texto.Length > 0;
    }
}
