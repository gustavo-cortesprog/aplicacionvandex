using Android.App;
using Android.App.Job;

namespace aplicacionvandex;

// Android llama este servicio aunque el usuario esté en otra aplicación.
[Service(Name = "com.companyname.aplicacionvandex.RadarJobService",
    Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
public sealed class RadarJobService : JobService
{
    private CancellationTokenSource? ejecucion;

    // Inicia una revisión con una señal de cancelación si el radar sigue activo.
    public override bool OnStartJob(JobParameters? parametros)
    {
        if (parametros is null || !RadarVantex.Ajustes.Activas) return false;
        var cancelacion = new CancellationTokenSource();
        ejecucion = cancelacion;
        _ = Consultar(parametros, cancelacion);
        return true;
    }

    // Cancela la consulta en curso cuando Android interrumpe el trabajo.
    public override bool OnStopJob(JobParameters? parametros)
    {
        ejecucion?.Cancel();
        return false; // La próxima ejecución de la tarea periódica será suficiente.
    }

    // Consulta el radar y avisa al programador cuando termina el trabajo que sigue activo.
    private async Task Consultar(JobParameters parametros, CancellationTokenSource cancelacion)
    {
        try { await RadarVantex.Revisar(cancelacion.Token); }
        catch (Exception error) { System.Diagnostics.Debug.WriteLine(error.GetType().Name); }
        finally
        {
            if (!cancelacion.IsCancellationRequested) JobFinished(parametros, false);
            if (ReferenceEquals(ejecucion, cancelacion)) ejecucion = null;
            cancelacion.Dispose();
        }
    }
}
