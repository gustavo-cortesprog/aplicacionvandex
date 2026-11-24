namespace aplicacionvandex;

public partial class MainPage : FlyoutPage
{
    // InitializeComponent une esta clase con MainPage.xaml y crea su menú.
    public MainPage()
    {
        InitializeComponent();
    }

    // Sustituye el contenido central y cierra el menú al tocar una opción.
    private void Mostrar(Page pagina)
    {
        Detail = new NavigationPage(pagina)
        {
            BarBackgroundColor = Color.FromArgb("#101722"),
            BarTextColor = Color.FromArgb("#F1F6FC")
        };
        IsPresented = false;
    }

    // Los eventos del menú crean la pantalla elegida y la pasan a Mostrar.
    private void OnPortafolioClicked(object? sender, EventArgs e) => Mostrar(new PanelPage());
    private void OnWebClicked(object? sender, EventArgs e) => Mostrar(new WebPage());
    private void OnAlertasClicked(object? sender, EventArgs e) => Mostrar(new AlertasPage());
    private void OnConexionClicked(object? sender, EventArgs e) => Mostrar(new ConexionPage());
}
