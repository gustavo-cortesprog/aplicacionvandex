namespace aplicacionvandex;

// Aplica las reglas al portafolio virtual, sin depender de una pantalla ni del sistema Android.
public static class MotorAlertas
{
    public static List<AvisoVantex> Evaluar(PortafolioMovil portafolio, AjustesAlertas ajustes, EstadoAlertas estado)
    {
        var nuevos = new List<AvisoVantex>();
        if (!ajustes.Activas) return nuevos;

        // Solo compara posiciones del modo elegido y elimina referencias de posiciones que ya no existen.
        var posiciones = portafolio.Posiciones.Where(p => p.Modo == ajustes.Modo).ToArray();
        var claves = posiciones.Select(p => p.Clave).ToHashSet();
        foreach (var clave in estado.Referencias.Keys.Where(c => !claves.Contains(c)).ToArray())
            estado.Referencias.Remove(clave);

        foreach (var posicion in posiciones)
        {
            if (posicion.Precio is not decimal precio || precio <= 0 || posicion.Actualizado is not DateTimeOffset fecha)
                continue;

            // En el modo de mercado se descartan precios guardados por el proveedor hace más de un día.
            if (posicion.Modo == "real" && portafolio.Consulta - fecha > TimeSpan.FromDays(1))
                continue;

            if (!estado.Referencias.TryGetValue(posicion.Clave, out var referencia)
                || referencia.Cantidad != posicion.Cantidad || referencia.Costo != posicion.Costo)
            {
                // Una compra o venta cambia la base. La siguiente cotización se compara con esta referencia.
                estado.Referencias[posicion.Clave] = new ReferenciaAlerta
                {
                    Precio = precio, Cantidad = posicion.Cantidad, Costo = posicion.Costo, UltimaCotizacion = fecha,
                    Zona = Zona(posicion.Rendimiento ?? 0, ajustes.Umbral)
                };
                continue;
            }

            // Evalúa cada cotización nueva una sola vez, aunque la API vuelva a entregar el mismo dato.
            if (fecha <= referencia.UltimaCotizacion) continue;
            referencia.UltimaCotizacion = fecha;

            decimal? cambio = ajustes.Regla == "precio"
                ? (precio - referencia.Precio) / referencia.Precio * 100
                : posicion.Rendimiento;
            if (!cambio.HasValue) continue;

            int zona = Zona(cambio.Value, ajustes.Umbral);
            if (ajustes.Regla == "precio")
            {
                if (zona == 0) continue;
                // Tras avisar por precio, este valor es el punto de partida para la próxima comparación.
                referencia.Precio = precio;
            }
            else
            {
                // Un umbral vuelve a activarse al salir de la zona, para evitar avisos repetidos.
                if (zona == referencia.Zona) continue;
                referencia.Zona = zona;
                if (zona == 0) continue;
            }

            string movimiento = ajustes.Regla == "precio"
                ? zona > 0 ? "Precio al alza" : "Precio a la baja"
                : zona > 0 ? "Ganancia virtual" : "Pérdida virtual";
            nuevos.Add(new AvisoVantex
            {
                Titulo = $"{posicion.Simbolo} · {movimiento}",
                Detalle = $"{cambio:+0.00;-0.00;0.00} % · {posicion.Fuente}",
                Fecha = portafolio.Consulta
            });
        }
        return nuevos;
    }

    // Distingue si el porcentaje alcanzó el umbral superior, el inferior o sigue dentro del margen.
    private static int Zona(decimal porcentaje, int umbral) =>
        porcentaje >= umbral ? 1 : porcentaje <= -umbral ? -1 : 0;
}
