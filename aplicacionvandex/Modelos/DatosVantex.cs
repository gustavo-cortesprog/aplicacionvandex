using System.Globalization;

namespace aplicacionvandex;

// Datos que entrega Vantex. El móvil solo recibe posiciones de la simulación.
public sealed class PortafolioMovil
{
    public List<PosicionMovil> Posiciones { get; set; } = new();
    public DateTimeOffset Consulta { get; set; }
}

// Contiene unidades, costo y cotización. Las propiedades calculadas preparan el resultado que muestra el panel.
public sealed class PosicionMovil
{
    public string Simbolo { get; set; } = "";
    public string Modo { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal? Precio { get; set; }
    public decimal? Resultado { get; set; }
    public DateTimeOffset? Actualizado { get; set; }

    public string Fuente => Modo == "demo" ? "Demostración" : "Mercado";
    // Separa las referencias de un mismo símbolo cuando existe en los dos modos.
    public string Clave => Simbolo + ":" + Modo;
    public decimal? Rendimiento => Costo > 0 && Resultado.HasValue ? Resultado.Value / Costo * 100 : null;
    public string Detalle => Fuente + " · " + Cantidad.ToString("0.########", CultureInfo.CurrentCulture) + " unidades";
    public string ResultadoTexto => Rendimiento.HasValue
        ? $"{Resultado:+0.00;-0.00;0.00} USD  ·  {Rendimiento:+0.00;-0.00;0.00} %"
        : "Cotización pendiente";
    public Color ColorResultado => Color.FromArgb(Resultado is > 0 ? "#7FE6B3" : Resultado is < 0 ? "#FF9B9B" : "#AABDD2");
}

// Ajustes elegidos en Alertas. Las claves de modo coinciden con las de la web.
public sealed class AjustesAlertas
{
    public bool Activas { get; set; }
    public string Modo { get; set; } = "demo";
    public string Regla { get; set; } = "rendimiento";
    public int Umbral { get; set; } = 5;
}

// Cada aviso conserva su fecha, activo, tipo de movimiento y porcentaje.
public sealed class AvisoVantex
{
    public string Titulo { get; set; } = "";
    public string Detalle { get; set; } = "";
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.Now;
    public string FechaTexto => Fecha.ToLocalTime().ToString("dd/MM HH:mm");
}

// Estado de comparación que se mantiene aunque Android cierre el proceso de la app.
public sealed class ReferenciaAlerta
{
    public decimal Precio { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Costo { get; set; }
    public DateTimeOffset UltimaCotizacion { get; set; }
    public int Zona { get; set; }
}

// Agrupa la referencia de cada posición con los avisos recientes.
public sealed class EstadoAlertas
{
    public Dictionary<string, ReferenciaAlerta> Referencias { get; set; } = new();
    public List<AvisoVantex> Historial { get; set; } = new();
}

// SecureStorage guarda la vinculación junta para que el token nunca viaje a otra URL.
public sealed class AccesoMovil
{
    public string Direccion { get; set; } = "";
    public string Token { get; set; } = "";
}
