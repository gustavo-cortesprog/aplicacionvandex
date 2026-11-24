using System.Text.Json;

namespace aplicacionvandex;

// Coordina las consultas de la pantalla y de Android para guardar un solo aviso por movimiento.
public static class RadarVantex
{
    private const string ClaveAjustes = "vantex.ajustes-alertas";
    private const string ClaveEstado = "vantex.estado-alertas";
    // El semáforo permite que el temporizador y los botones consulten o escriban de uno en uno.
    private static readonly SemaphoreSlim turno = new(1, 1);

    public static AjustesAlertas Ajustes => Leer<AjustesAlertas>(ClaveAjustes);
    public static List<AvisoVantex> Historial => Leer<EstadoAlertas>(ClaveEstado).Historial;

    // Guarda las preferencias y vuelve a tomar referencias si cambió alguna regla.
    public static async Task GuardarAjustes(AjustesAlertas ajustes)
    {
        await turno.WaitAsync();
        try
        {
            var anterior = Ajustes;
            Preferences.Default.Set(ClaveAjustes, JsonSerializer.Serialize(ajustes));
            if (anterior.Modo == ajustes.Modo && anterior.Regla == ajustes.Regla
                && anterior.Umbral == ajustes.Umbral && anterior.Activas == ajustes.Activas) return;
            var estado = Leer<EstadoAlertas>(ClaveEstado);
            estado.Referencias.Clear();
            GuardarEstado(estado);
        }
        finally { turno.Release(); }
    }

    // Elimina las referencias y el historial guardados para la cuenta anterior.
    public static async Task Reiniciar()
    {
        await turno.WaitAsync();
        try { Preferences.Default.Remove(ClaveEstado); }
        finally { turno.Release(); }
    }

    // Consulta posiciones, evalúa las reglas activas y conserva hasta 30 avisos.
    public static async Task<PortafolioMovil> Revisar(CancellationToken cancelacion = default)
    {
        await turno.WaitAsync(cancelacion);
        try
        {
            var acceso = await ConexionVantex.ObtenerAcceso()
                ?? throw new InvalidOperationException("Vincula tu portafolio en el apartado Conexión.");
            var portafolio = await ConexionVantex.Consultar(acceso, cancelacion);
            // Descarta una respuesta pendiente si la vinculación cambió durante la consulta.
            if ((await ConexionVantex.ObtenerAcceso())?.Token != acceso.Token)
                throw new InvalidOperationException("La vinculación cambió. Actualiza el portafolio.");

            var ajustes = Ajustes;
            if (!ajustes.Activas) return portafolio;
            var estado = Leer<EstadoAlertas>(ClaveEstado);
            var avisos = MotorAlertas.Evaluar(portafolio, ajustes, estado);
            estado.Historial = avisos.Concat(estado.Historial).Take(30).ToList();
            GuardarEstado(estado);

            foreach (var aviso in avisos)
                NotificacionesVantex.Mostrar(aviso.Titulo, aviso.Detalle);
            return portafolio;
        }
        finally { turno.Release(); }
    }

    // Reconstruye el objeto desde las preferencias del dispositivo. Usa valores iniciales si falta o no se puede leer.
    private static T Leer<T>(string clave) where T : new()
    {
        try { return JsonSerializer.Deserialize<T>(Preferences.Default.Get(clave, "{}")) ?? new T(); }
        catch (JsonException) { return new T(); }
    }

    private static void GuardarEstado(EstadoAlertas estado) =>
        Preferences.Default.Set(ClaveEstado, JsonSerializer.Serialize(estado));
}
