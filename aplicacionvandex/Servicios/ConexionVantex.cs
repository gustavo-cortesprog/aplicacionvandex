using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace aplicacionvandex;

// Conexión con las rutas de Vantex. Los botones del móvil no envían compras ni ventas.
public static class ConexionVantex
{
    private const string ClaveServidor = "vantex.servidor";
    private const string ClaveAcceso = "vantex.vinculo";
    private static readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(25)
    };
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);

    public static string Direccion => Preferences.Default.Get(ClaveServidor, DireccionInicial);

    private static string DireccionInicial
    {
        get
        {
#if DEBUG
            return DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5175" : "http://localhost:5175";
#else
            return "";
#endif
        }
    }

    // Las copias de depuración aceptan solo HTTP local. En cualquier otro caso se usa HTTPS.
    public static bool LeerDireccion(string? texto, out Uri direccion)
    {
        direccion = null!;
        if (!Uri.TryCreate(texto?.Trim(), UriKind.Absolute, out var candidata)
            || candidata.UserInfo.Length != 0 || candidata.Query.Length != 0 || candidata.Fragment.Length != 0
            || candidata.AbsolutePath != "/")
            return false;

        bool permitida = candidata.Scheme == Uri.UriSchemeHttps;
#if DEBUG
        permitida |= candidata.Scheme == Uri.UriSchemeHttp
            && (candidata.IsLoopback || candidata.Host == "10.0.2.2");
#endif
        if (!permitida) return false;
        direccion = new Uri(candidata.GetLeftPart(UriPartial.Authority) + "/");
        return true;
    }

    public static void GuardarDireccion(Uri direccion) =>
        Preferences.Default.Set(ClaveServidor, direccion.GetLeftPart(UriPartial.Authority));

    // Verifica que la dirección abra un servidor Vantex con las rutas para móviles.
    public static async Task Probar(Uri servidor, CancellationToken cancelacion)
    {
        using var respuesta = await http.GetAsync(new Uri(servidor, "/api/estado"), cancelacion);
        respuesta.EnsureSuccessStatusCode();
        using var contenido = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(cancelacion));
        var datos = contenido.RootElement;
        if (!datos.TryGetProperty("version", out var version) || version.GetString() != "Vantex"
            || !datos.TryGetProperty("movil", out var movil) || movil.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException("La dirección debe apuntar a Vantex con la conexión móvil habilitada.");
    }

    // Canjea el código de un solo uso y guarda únicamente el token de lectura.
    public static async Task Vincular(Uri servidor, string codigo, CancellationToken cancelacion)
    {
        await Probar(servidor, cancelacion);
        using var peticion = new HttpRequestMessage(HttpMethod.Post, new Uri(servidor, "/api/movil/vincular"));
        peticion.Headers.Add("X-Vantex", "1");
        peticion.Content = JsonContent.Create(new { codigo });
        using var respuesta = await http.SendAsync(peticion, cancelacion);
        if (respuesta.StatusCode == HttpStatusCode.BadRequest)
            throw new InvalidOperationException("El código venció, ya se usó o es incorrecto.");
        if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("Hay demasiados intentos. Espera un minuto.");
        respuesta.EnsureSuccessStatusCode();

        using var datos = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(cancelacion));
        var token = datos.RootElement.GetProperty("token").GetString();
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Vantex no devolvió una vinculación.");
        var acceso = new AccesoMovil { Direccion = servidor.GetLeftPart(UriPartial.Authority), Token = token };
        await SecureStorage.Default.SetAsync(ClaveAcceso, JsonSerializer.Serialize(acceso, json));
        GuardarDireccion(servidor);
    }

    public static async Task<AccesoMovil?> ObtenerAcceso()
    {
        var guardado = await SecureStorage.Default.GetAsync(ClaveAcceso);
        return guardado is null ? null : JsonSerializer.Deserialize<AccesoMovil>(guardado, json);
    }

    public static void Desvincular() => SecureStorage.Default.Remove(ClaveAcceso);

    // El token se manda solo al origen guardado y nunca se agrega a una dirección del navegador.
    public static async Task<PortafolioMovil> Consultar(AccesoMovil acceso, CancellationToken cancelacion)
    {
        if (!LeerDireccion(acceso.Direccion, out var servidor)) throw new InvalidOperationException("Dirección de Vantex inválida.");
        using var peticion = new HttpRequestMessage(HttpMethod.Get, new Uri(servidor, "/api/movil/portafolio"));
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);
        using var respuesta = await http.SendAsync(peticion, cancelacion);
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("La vinculación venció. Genera otro código en Mi portafolio.");
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<PortafolioMovil>(json, cancelacion)
            ?? throw new InvalidOperationException("Vantex no devolvió el portafolio.");
    }

    public static string MensajeError(Exception error) => error switch
    {
        HttpRequestException => "No se pudo conectar. Revisa la dirección y que Vantex esté ejecutándose.",
        OperationCanceledException => "La consulta tardó demasiado. Intenta otra vez.",
        InvalidOperationException => error.Message,
        _ => "No se pudo completar la operación."
    };
}
